using System.Text;
using System.Text.Json;

// ============================================================
// Band 자동 게시 프로그램
//
// 동작 방식:
//  1. appsettings.json 에서 설정을 읽는다.
//  2. RefreshToken 으로 새 AccessToken 을 발급받는다.
//     (밴드가 RefreshToken 을 재발급해주면 파일에 다시 저장한다)
//  3. 오늘 이미 게시했는지 last_posted.txt 로 확인한다.
//     (스케줄러가 늦게 실행되거나 여러 번 실행돼도 중복 게시 방지)
//  4. 아직이면 글을 올리고 오늘 날짜를 기록한다.
//
// 사전 준비 (최초 1회, 프로그램이 아니라 사람이 직접 해야 함):
//  1) https://developers.band.us 에서 앱 등록 → Client ID/Secret 발급
//  2) 브라우저로 아래 주소 접속 후 로그인/권한 허용:
//     https://auth.band.us/oauth2/authorize
//       ?response_type=code
//       &client_id={ClientId}
//       &redirect_uri={등록한_redirect_uri}
//     리다이렉트된 주소의 ?code=xxxx 값을 확인
//  3) 아래 요청으로 최초 AccessToken/RefreshToken 발급:
//     POST https://auth.band.us/oauth2/token
//       grant_type=authorization_code
//       client_id={ClientId}
//       client_secret={ClientSecret}
//       code={위에서_받은_code}
//       redirect_uri={등록한_redirect_uri}
//     응답의 refresh_token 값을 appsettings.json 의 RefreshToken 에 넣는다.
// ============================================================

var baseDir = AppContext.BaseDirectory;
var currentDir = Directory.GetCurrentDirectory();

// 실행 파일 폴더(bin\...) 또는 현재 작업 폴더, 둘 중 있는 곳에서 찾는다
string ResolvePath(string fileName)
{
    var inBaseDir = Path.Combine(baseDir, fileName);
    if (File.Exists(inBaseDir)) return inBaseDir;

    var inCurrentDir = Path.Combine(currentDir, fileName);
    if (File.Exists(inCurrentDir)) return inCurrentDir;

    // 못 찾으면 기본값(baseDir 기준)을 반환 - 이후 로직에서 존재 여부를 다시 체크함
    return inBaseDir;
}

var settingsPath = ResolvePath("appsettings.json");
var markerPath = Path.Combine(baseDir, "last_posted.txt");
var logPath = Path.Combine(baseDir, "band_autopost.log");

void Log(string msg)
{
    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

try
{
    if (!File.Exists(settingsPath))
    {
        Log($"appsettings.json 파일을 찾을 수 없습니다. 확인한 위치: {baseDir} 그리고 {currentDir}");
        return;
    }

    var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath))
        ?? throw new Exception("설정 파일을 읽을 수 없습니다.");

    // 특정 요일 건너뛰기 (SkipWeekdays 예: ["Saturday", "Sunday"])
    var todayWeekday = DateTime.Now.DayOfWeek.ToString(); // "Monday", "Tuesday" ... (문화권과 무관하게 항상 영문)
    if (settings.SkipWeekdays.Any(d => string.Equals(d, todayWeekday, StringComparison.OrdinalIgnoreCase)))
    {
        Log($"오늘은 {todayWeekday} 이라 건너뛰는 요일입니다. 게시하지 않습니다.");
        return;
    }

    // 날짜 등을 실제 값으로 치환해 최종 게시 내용을 만든다
    var koreanWeekdays = new[] { "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" };
    var dateText = $"{DateTime.Now:yyyy.MM.dd} ({koreanWeekdays[(int)DateTime.Now.DayOfWeek]})";
    var postContent = settings.PostContentTemplate.Replace("{date}", dateText);

    // 오늘 이미 올렸는지 확인 (중복 게시 방지 + PC가 늦게 켜져도 안전)
    var today = DateTime.Now.ToString("yyyy-MM-dd");
    if (File.Exists(markerPath) && File.ReadAllText(markerPath).Trim() == today)
    {
        Log($"오늘({today})은 이미 게시했습니다. 종료합니다.");
        return;
    }

    using var http = new HttpClient();

    string accessToken;

    if (settings.DryRun)
    {
        // DryRun 에서는 인증 서버 호출 자체를 건너뜁니다.
        // 지금 막혀있는 401 인증 문제와 무관하게, 설정 읽기 / 중복 게시 방지 /
        // 로그 기록 / 스케줄러 동작 같은 나머지 로직만 먼저 검증할 수 있습니다.
        Log("[DRY RUN] 토큰 갱신 API 호출을 건너뜁니다. (실제 인증 문제와 무관하게 테스트)");
        accessToken = "dry-run-fake-token";
    }
    else
    {
        // 1) refresh_token 으로 access_token 갱신
        // 밴드 인증 서버는 client_id/client_secret 을 HTTP Basic 인증 헤더로 요구합니다.
        Log("액세스 토큰 갱신 중...");
        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://auth.band.us/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = settings.RefreshToken,
            })
        };
        tokenRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);

        var tokenResp = await http.SendAsync(tokenRequest);

        var tokenBody = await tokenResp.Content.ReadAsStringAsync();
        if (!tokenResp.IsSuccessStatusCode)
        {
            Log($"토큰 갱신 실패 ({(int)tokenResp.StatusCode}): {tokenBody}");
            Log("refresh_token 이 만료되었을 수 있습니다. 최초 인증 과정을 다시 진행해야 합니다.");
            return;
        }

        var tokenData = JsonSerializer.Deserialize<TokenResponse>(tokenBody)
            ?? throw new Exception("토큰 응답을 파싱할 수 없습니다.");

        // 밴드가 새 refresh_token 을 내려주면 갱신해서 저장 (없으면 기존 값 유지)
        if (!string.IsNullOrEmpty(tokenData.refresh_token) &&
            tokenData.refresh_token != settings.RefreshToken)
        {
            settings.RefreshToken = tokenData.refresh_token;
            File.WriteAllText(settingsPath,
                JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            Log("새로운 refresh_token 을 저장했습니다.");
        }

        accessToken = tokenData.access_token;
    }

    // 2) 게시글 작성
    if (settings.DryRun)
    {
        // 글쓰기 권한 심사 대기 중이거나 테스트 단계일 때: 실제 전송 없이 로직만 확인
        Log("[DRY RUN] 실제로 게시하지 않습니다. 아래 내용으로 전송될 예정입니다:");
        Log($"[DRY RUN] band_key = {settings.BandKey}");
        Log($"[DRY RUN] content  = {postContent}");
        Log($"[DRY RUN] do_push  = {settings.DoPush}");
        File.WriteAllText(markerPath, today);
        return;
    }

    Log("게시글 작성 중...");
    var postResp = await http.PostAsync("https://openapi.band.us/v2.2/band/post/create",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["access_token"] = accessToken,
            ["band_key"] = settings.BandKey,
            ["content"] = postContent,
            ["do_push"] = settings.DoPush ? "true" : "false",
        }));

    var postBody = await postResp.Content.ReadAsStringAsync();
    if (!postResp.IsSuccessStatusCode)
    {
        Log($"게시글 작성 실패 ({(int)postResp.StatusCode}): {postBody}");
        return;
    }

    File.WriteAllText(markerPath, today);
    Log("게시글 작성 완료!");
    Log(postBody);
}
catch (Exception ex)
{
    Log($"오류 발생: {ex.Message}");
}

class Settings
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string BandKey { get; set; } = "";
    public string PostContentTemplate { get; set; } = "";
    public List<string> SkipWeekdays { get; set; } = new();
    public bool DoPush { get; set; } = true;
    public bool DryRun { get; set; } = false;
}

class TokenResponse
{
    public string access_token { get; set; } = "";
    public string? refresh_token { get; set; }
}