# Band 자동 게시 프로그램

매일 정해진 시간에 밴드에 자동으로 글을 올리는 콘솔 프로그램입니다.

## 1. 사전 준비 (최초 1회, 직접 진행)

1. https://developers.band.us 에서 앱 등록 → **Client ID / Client Secret** 발급
   - Redirect URI는 아무 임의 주소(예: `http://localhost:5000/callback`)로 등록해도 됩니다.
2. 브라우저 주소창에 아래 URL을 입력 → 로그인 후 "권한 허용" 클릭
   ```
   https://auth.band.us/oauth2/authorize?response_type=code&client_id=여기에_ClientId&redirect_uri=등록한_redirect_uri
   ```
3. 리다이렉트된 주소창의 `?code=...` 값을 복사
4. 아래 요청을 (Postman이나 curl로) 한 번 보내서 최초 토큰 발급
   ```
   POST https://auth.band.us/oauth2/token
   grant_type=authorization_code
   client_id=여기에_ClientId
   client_secret=여기에_ClientSecret
   code=위에서_복사한_code
   redirect_uri=등록한_redirect_uri
   ```
5. 응답으로 받은 `refresh_token`을 `appsettings.json`의 `RefreshToken`에 붙여넣기
6. `band_key`는 밴드 웹사이트에서 해당 밴드 페이지 URL(`https://band.us/band/XXXXXXXX`)의 `XXXXXXXX` 부분입니다.

## 2. 실행

```
dotnet run
```

빌드된 실행 파일(`BandAutoPost.exe`)을 원하는 폴더에 두고 실행해도 됩니다.

## 3. 매일 자동 실행 설정 (Windows 작업 스케줄러)

PC를 항상 켜두지 않는다고 하셨으니, 아래 옵션을 꼭 확인하세요.

1. `작업 스케줄러` 실행 → `기본 작업 만들기`
2. 트리거: 매일, 원하는 시각 지정
3. 동작: 프로그램 시작 → `BandAutoPost.exe` 경로 지정
4. **조건 탭**에서 "컴퓨터를 깨워서 이 작업 실행" 체크
5. **설정 탭**에서 "예약된 시작을 놓친 경우 가능한 한 빨리 작업 시작" 체크
   - 이 옵션 덕분에 PC가 꺼져 있어서 예정 시각을 놓쳐도, 다음에 PC를 켰을 때 자동으로 실행됩니다.
   - 프로그램 자체에도 "오늘 이미 올렸으면 건너뛰기" 로직이 들어있어 중복 게시는 안전합니다.

## 4. 참고

- `refresh_token`은 밴드 정책상 언젠가 만료될 수 있습니다. 실행 로그(`band_autopost.log`)에 토큰 갱신 실패가 뜨면 1번 과정을 다시 진행하세요.
- 매일 다른 내용을 올리고 싶어지면 `appsettings.json`의 `PostContent`를 실행 전에 바꾸거나, 날짜별 문구 배열을 코드에 추가하면 됩니다.
