using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ResumeAI.Domain.Entities;
using ResumeAI.Domain.Enums;
using ResumeAI.Domain.Interfaces;

namespace ResumeAI.Infrastructure.LLM;

/// <summary>
/// LLM servis implementasyonu - OpenAI, Gemini ve Qwen destekler
/// </summary>
public class LLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private LLMProvider _currentProvider;
    private string _apiKey = string.Empty;
    private string _baseUrl = string.Empty;
    private string _model = string.Empty;

    public LLMProvider CurrentProvider => _currentProvider;
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public LLMService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void Configure(LLMProvider provider, string apiKey)
    {
        _currentProvider = provider;
        _apiKey = apiKey;

        switch (provider)
        {
            case LLMProvider.OpenAI:
                _baseUrl = "https://api.openai.com/v1/chat/completions";
                _model = "gpt-4o-mini";
                break;
            case LLMProvider.Gemini:
                _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
                _model = "gemini-2.0-flash";
                break;
            case LLMProvider.Qwen:
                _baseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
                _model = "qwen-turbo";
                break;
        }
    }

    /// <summary>
    /// LLM yanıtından JSON bloğunu çıkarır
    /// </summary>
    private string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "{}";

        // Önce ```json ... ``` bloğunu ara
        var jsonBlockMatch = Regex.Match(response, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (jsonBlockMatch.Success)
        {
            return jsonBlockMatch.Groups[1].Value.Trim();
        }

        // { ile başlayıp } ile biten kısmı bul
        var startIndex = response.IndexOf('{');
        var endIndex = response.LastIndexOf('}');
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }

        return response;
    }

    public async Task<Candidate> ParseResumeAsync(string content, string fileName)
    {
        var prompt = BuildParsePrompt(content);
        var response = await SendRequestAsync(prompt);

        try
        {
            // JSON'u yanıttan çıkar
            var jsonContent = ExtractJsonFromResponse(response);
            
            var parsed = JsonSerializer.Deserialize<ParsedResumeResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

            return new Candidate
            {
                FullName = parsed?.FullName ?? "Bilinmeyen",
                Email = parsed?.Email ?? "",
                Phone = parsed?.Phone ?? "",
                FileName = fileName,
                RawContent = content,
                Skills = parsed?.Skills?.Select(s => new Skill
                {
                    Name = s.Name,
                    YearsOfExperience = s.YearsOfExperience,
                    Level = s.Level
                }).ToList() ?? new List<Skill>(),
                Experiences = parsed?.Experiences?.Select(e => new Experience
                {
                    CompanyName = e.CompanyName,
                    Position = e.Position,
                    StartDate = ParseDate(e.StartDate),
                    EndDate = string.IsNullOrEmpty(e.EndDate) ? null : ParseDate(e.EndDate),
                    Description = e.Description ?? "",
                    IsCurrent = e.IsCurrent
                }).ToList() ?? new List<Experience>(),
                Education = parsed?.Education != null ? new Education
                {
                    Institution = parsed.Education.Institution,
                    Degree = parsed.Education.Degree,
                    FieldOfStudy = parsed.Education.FieldOfStudy,
                    GraduationYear = parsed.Education.GraduationYear
                } : null
            };
        }
        catch (Exception ex)
        {
            // Hata durumunda CV içeriğini RawContent'te saklayarak döndür
            // Analiz sırasında bu içerik kullanılacak ve isim çıkarılacak
            // İsim olarak dosya adından temel bir isim çıkarmaya çalış
            var baseName = Path.GetFileNameWithoutExtension(fileName)
                .Replace("_", " ")
                .Replace("-", " ");
            
            return new Candidate
            {
                FullName = string.Empty, // Boş bırak, analiz sırasında doldurulacak
                FileName = fileName,
                RawContent = content,
                IsParsed = false
            };
        }
    }

    public async Task<AnalysisResult> AnalyzeCandidateAsync(Candidate candidate, JobRequirement requirement)
    {
        // Eğer CV parse edilemedi ise, raw content üzerinden direkt analiz yap
        var prompt = !candidate.IsParsed || string.IsNullOrEmpty(candidate.FullName)
            ? BuildDirectAnalysisPrompt(candidate.RawContent, requirement)
            : BuildAnalysisPrompt(candidate, requirement);
            
        var response = await SendRequestAsync(prompt);

        try
        {
            // JSON'u yanıttan çıkar
            var jsonContent = ExtractJsonFromResponse(response);
            
            var analysis = JsonSerializer.Deserialize<AnalysisResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

            if (analysis == null)
            {
                throw new Exception("Analiz yanıtı parse edilemedi");
            }

            // Ağırlıklı toplam skor hesapla
            var totalScore = (analysis.SkillsScore * requirement.SkillsWeight / 100.0) +
                           (analysis.ExperienceScore * requirement.ExperienceWeight / 100.0) +
                           (analysis.EducationScore * requirement.EducationWeight / 100.0);

            return new AnalysisResult
            {
                CandidateId = candidate.Id,
                SkillsScore = analysis.SkillsScore,
                ExperienceScore = analysis.ExperienceScore,
                EducationScore = analysis.EducationScore,
                TotalScore = Math.Round(totalScore, 2),
                SkillsAnalysis = new SkillsAnalysis
                {
                    MatchedSkills = analysis.MatchedSkills ?? new List<string>(),
                    MissingSkills = analysis.MissingSkills ?? new List<string>(),
                    MatchedCount = analysis.MatchedSkills?.Count ?? 0,
                    RequiredCount = requirement.RequiredSkills.Count
                },
                ExperienceAnalysis = new ExperienceAnalysis
                {
                    TotalYearsOfExperience = analysis.TotalYearsExperience,
                    RequiredYears = requirement.MinYearsOfExperience,
                    NumberOfCompanies = analysis.NumberOfCompanies,
                    AverageYearsPerCompany = analysis.AverageYearsPerCompany,
                    HasRelevantExperience = analysis.HasRelevantExperience
                },
                EducationAnalysis = new EducationAnalysis
                {
                    HasRequiredDegree = analysis.HasRequiredDegree,
                    IsRelevantField = analysis.IsRelevantField,
                    ActualDegree = analysis.ActualDegree ?? "",
                    ActualField = analysis.ActualField ?? ""
                },
                AISummary = analysis.Summary ?? "",
                Strengths = analysis.Strengths ?? "",
                Weaknesses = analysis.Weaknesses ?? "",
                // Aday bilgilerini güncelle
                CandidateName = analysis.CandidateName,
                CandidateEmail = analysis.CandidateEmail,
                CandidatePhone = analysis.CandidatePhone
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Analiz sırasında hata oluştu: {ex.Message}");
        }
    }

    private async Task<string> SendRequestAsync(string prompt)
    {
        return _currentProvider switch
        {
            LLMProvider.OpenAI => await SendOpenAIRequestAsync(prompt),
            LLMProvider.Gemini => await SendGeminiRequestAsync(prompt),
            LLMProvider.Qwen => await SendQwenRequestAsync(prompt),
            _ => throw new NotSupportedException($"Provider desteklenmiyor: {_currentProvider}")
        };
    }

    private async Task<string> SendOpenAIRequestAsync(string prompt)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Sen bir CV analiz uzmanısın. Yanıtlarını sadece JSON formatında ver." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI API hatası: {content}");
        }

        var result = JsonSerializer.Deserialize<OpenAIResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
    }

    private async Task<string> SendGeminiRequestAsync(string prompt)
    {
        var url = $"{_baseUrl}?key={_apiKey}";
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"Sen bir CV analiz uzmanısın. Yanıtlarını sadece JSON formatında ver.\n\n{prompt}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                responseMimeType = "application/json"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API hatası: {content}");
        }

        var result = JsonSerializer.Deserialize<GeminiResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
    }

    private async Task<string> SendQwenRequestAsync(string prompt)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Sen bir CV analiz uzmanısın. Yanıtlarını sadece JSON formatında ver." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Qwen API hatası: {content}");
        }

        // Qwen OpenAI-compatible format kullanır
        var result = JsonSerializer.Deserialize<OpenAIResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
    }

    private string BuildParsePrompt(string cvContent)
    {
        return $@"Aşağıdaki CV içeriğini analiz et ve JSON formatında çıkar.

CV İçeriği:
{cvContent}

Yanıt formatı (JSON):
{{
    ""fullName"": ""Adayın tam adı"",
    ""email"": ""E-posta adresi"",
    ""phone"": ""Telefon numarası"",
    ""skills"": [
        {{
            ""name"": ""Yetenek adı"",
            ""yearsOfExperience"": 0,
            ""level"": ""Beginner/Intermediate/Advanced/Expert""
        }}
    ],
    ""experiences"": [
        {{
            ""companyName"": ""Şirket adı"",
            ""position"": ""Pozisyon"",
            ""startDate"": ""YYYY-MM"",
            ""endDate"": ""YYYY-MM veya null (devam ediyorsa)"",
            ""description"": ""Kısa açıklama"",
            ""isCurrent"": true/false
        }}
    ],
    ""education"": {{
        ""institution"": ""Okul adı"",
        ""degree"": ""Lisans/Yüksek Lisans/Doktora"",
        ""fieldOfStudy"": ""Bölüm"",
        ""graduationYear"": 2020
    }}
}}";
    }

    private string BuildAnalysisPrompt(Candidate candidate, JobRequirement requirement)
    {
        var candidateSkills = string.Join(", ", candidate.Skills.Select(s => s.Name));
        var requiredSkills = string.Join(", ", requirement.RequiredSkills);
        var preferredSkills = string.Join(", ", requirement.PreferredSkills);
        var preferredFields = string.Join(", ", requirement.PreferredFieldsOfStudy);

        var totalExperienceYears = candidate.Experiences.Sum(e => e.DurationInMonths) / 12.0;

        return $@"Aşağıdaki adayı iş gereksinimlerine göre analiz et ve puanla.

## Aday Bilgileri
- Ad: {candidate.FullName}
- Yetenekler: {candidateSkills}
- Toplam Tecrübe: {totalExperienceYears:F1} yıl
- Çalıştığı Şirket Sayısı: {candidate.Experiences.Count}
- Eğitim: {candidate.Education?.Degree ?? "Belirtilmemiş"} - {candidate.Education?.FieldOfStudy ?? "Belirtilmemiş"}

## İş Gereksinimleri
- Pozisyon: {requirement.JobTitle}
- Açıklama: {requirement.Description}
- Zorunlu Yetenekler: {requiredSkills}
- Tercih Edilen Yetenekler: {preferredSkills}
- Minimum Tecrübe: {requirement.MinYearsOfExperience} yıl
- Maksimum Tecrübe: {requirement.MaxYearsOfExperience?.ToString() ?? "Belirtilmemiş"} yıl
- Zorunlu Eğitim: {requirement.RequiredDegree}
- Tercih Edilen Bölümler: {preferredFields}

## Puanlama Kriterleri
- Yetenekler Ağırlığı: %{requirement.SkillsWeight}
- Tecrübe Ağırlığı: %{requirement.ExperienceWeight}
- Eğitim Ağırlığı: %{requirement.EducationWeight}

Yanıt formatı (JSON):
{{
    ""skillsScore"": 0-100 arası puan,
    ""experienceScore"": 0-100 arası puan,
    ""educationScore"": 0-100 arası puan,
    ""matchedSkills"": [""eşleşen yetenekler listesi""],
    ""missingSkills"": [""eksik yetenekler listesi""],
    ""totalYearsExperience"": sayı,
    ""numberOfCompanies"": sayı,
    ""averageYearsPerCompany"": ondalık sayı,
    ""hasRelevantExperience"": true/false,
    ""hasRequiredDegree"": true/false,
    ""isRelevantField"": true/false,
    ""actualDegree"": ""adayın eğitim seviyesi"",
    ""actualField"": ""adayın bölümü"",
    ""summary"": ""Adayın genel değerlendirmesi (2-3 cümle)"",
    ""strengths"": ""Güçlü yönleri"",
    ""weaknesses"": ""Zayıf yönleri/eksikleri""
}}

Puanlama kuralları:
- skillsScore: Zorunlu yeteneklerin kaçı var? (matchedSkills.count / requiredSkills.count * 100)
- experienceScore: Tecrübe yılı yeterliyse 100, eksikse orantılı puan
- educationScore: Eğitim seviyesi ve bölüm uygunluğuna göre";
    }

    private string BuildDirectAnalysisPrompt(string rawCvContent, JobRequirement requirement)
    {
        var requiredSkills = string.Join(", ", requirement.RequiredSkills);
        var preferredSkills = string.Join(", ", requirement.PreferredSkills);
        var preferredFields = string.Join(", ", requirement.PreferredFieldsOfStudy);

        return $@"Aşağıdaki CV içeriğini oku, adayın bilgilerini çıkar ve iş gereksinimlerine göre analiz et.

## CV İçeriği (Ham Metin)
{rawCvContent}

## İş Gereksinimleri
- Pozisyon: {requirement.JobTitle}
- Açıklama: {requirement.Description}
- Zorunlu Yetenekler: {requiredSkills}
- Tercih Edilen Yetenekler: {preferredSkills}
- Minimum Tecrübe: {requirement.MinYearsOfExperience} yıl
- Maksimum Tecrübe: {requirement.MaxYearsOfExperience?.ToString() ?? "Belirtilmemiş"} yıl
- Zorunlu Eğitim: {requirement.RequiredDegree}
- Tercih Edilen Bölümler: {preferredFields}

## Puanlama Kriterleri
- Yetenekler Ağırlığı: %{requirement.SkillsWeight}
- Tecrübe Ağırlığı: %{requirement.ExperienceWeight}
- Eğitim Ağırlığı: %{requirement.EducationWeight}

CV içeriğinden aşağıdaki bilgileri çıkar ve iş gereksinimlerine göre puanla.
SADECE JSON formatında yanıt ver, başka hiçbir şey yazma:

{{
    ""candidateName"": ""adayın adı soyadı"",
    ""candidateEmail"": ""e-posta adresi veya null"",
    ""candidatePhone"": ""telefon numarası veya null"",
    ""skillsScore"": 0-100 arası puan,
    ""experienceScore"": 0-100 arası puan,
    ""educationScore"": 0-100 arası puan,
    ""matchedSkills"": [""CV'de bulunan ve iş gereksinimlerinde istenen yetenekler""],
    ""missingSkills"": [""iş gereksinimlerinde istenen ama CV'de olmayan yetenekler""],
    ""totalYearsExperience"": toplam tecrübe yılı (sayı),
    ""numberOfCompanies"": çalıştığı şirket sayısı (sayı),
    ""averageYearsPerCompany"": ortalama şirket başına yıl (ondalık sayı),
    ""hasRelevantExperience"": ilgili deneyimi var mı (true/false),
    ""hasRequiredDegree"": gerekli eğitimi var mı (true/false),
    ""isRelevantField"": ilgili bölüm mü (true/false),
    ""actualDegree"": ""adayın eğitim seviyesi"",
    ""actualField"": ""adayın bölümü"",
    ""summary"": ""Adayın genel değerlendirmesi (2-3 cümle)"",
    ""strengths"": ""Güçlü yönleri"",
    ""weaknesses"": ""Zayıf yönleri/eksikleri""
}}

Puanlama kuralları:
- skillsScore: Zorunlu yeteneklerin kaçı CV'de var? Eşleşen sayısı / zorunlu yetenek sayısı * 100
- experienceScore: Tecrübe yılı yeterliyse 100, eksikse orantılı puan (aday_yılı / gereken_yıl * 100)
- educationScore: Eğitim seviyesi ve bölüm uygunluğuna göre 0-100";
    }

    private DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParse(dateStr, out var date))
            return date;
        
        // YYYY-MM formatını dene
        if (dateStr.Length == 7 && dateStr[4] == '-')
        {
            if (int.TryParse(dateStr.Substring(0, 4), out var year) &&
                int.TryParse(dateStr.Substring(5, 2), out var month))
            {
                return new DateTime(year, month, 1);
            }
        }

        return DateTime.MinValue;
    }
}

#region Response Models

internal class ParsedResumeResponse
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<ParsedSkill>? Skills { get; set; }
    public List<ParsedExperience>? Experiences { get; set; }
    public ParsedEducation? Education { get; set; }
}

internal class ParsedSkill
{
    public string Name { get; set; } = "";
    public int YearsOfExperience { get; set; }
    public string Level { get; set; } = "";
}

internal class ParsedExperience
{
    public string CompanyName { get; set; } = "";
    public string Position { get; set; } = "";
    public string StartDate { get; set; } = "";
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsCurrent { get; set; }
}

internal class ParsedEducation
{
    public string Institution { get; set; } = "";
    public string Degree { get; set; } = "";
    public string FieldOfStudy { get; set; } = "";
    public int? GraduationYear { get; set; }
}

internal class AnalysisResponse
{
    public double SkillsScore { get; set; }
    public double ExperienceScore { get; set; }
    public double EducationScore { get; set; }
    public List<string>? MatchedSkills { get; set; }
    public List<string>? MissingSkills { get; set; }
    public double TotalYearsExperience { get; set; }
    public double NumberOfCompanies { get; set; }
    public double AverageYearsPerCompany { get; set; }
    public bool HasRelevantExperience { get; set; }
    public bool HasRequiredDegree { get; set; }
    public bool IsRelevantField { get; set; }
    public string? ActualDegree { get; set; }
    public string? ActualField { get; set; }
    public string? Summary { get; set; }
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    // Aday bilgileri (CV parse edilemediğinde)
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
}

internal class OpenAIResponse
{
    public List<OpenAIChoice>? Choices { get; set; }
}

internal class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
}

internal class OpenAIMessage
{
    public string? Content { get; set; }
}

internal class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

internal class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

internal class GeminiPart
{
    public string? Text { get; set; }
}

#endregion
