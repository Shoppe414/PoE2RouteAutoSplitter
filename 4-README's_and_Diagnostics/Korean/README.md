# PoE2 Route AutoSplitter

**Path of Exile 2 캠페인 스피드런**을 위한 설정 도구이자 LiveSplit 오토스플리터입니다.

현재 릴리스: **v3.0.0 Release Candidate**

PoE2 Route AutoSplitter는 다음과 같은 미리 만든 경로와 사용자 지정 경로를 제공합니다.

* 탐험 / 지역 완료
* Boss Rush
* 탐험 + Boss Rush 결합
* Campaign Any%
* Campaign 100%
* 필수 캠페인 보스만
* 0.5 Pinnacle 보스
* Temple of Chaos
* Trial of the Sekhemas
* 사용자 정의 경로
* Maps

포함된 **PoE2RouteSetup** 애플리케이션이 대부분의 설정을 처리합니다.

일시정지 메뉴를 열 때 게임과 LiveSplit 타이머를 동기화하여 일시정지할 수 있습니다.
LiveSplit의 Game Time 옵션은 로딩 시간을 제외하고, 해당 옵션이 활성화된 경우 수동 일시정지 시간도 제외합니다.

스크린샷: https://imgur.com/a/VgiRn6o

---
# 런 정책

특정 규칙 세트에 최대한 종속되지 않도록 설계했습니다. 플레이어는 자신의 런 규칙과 사용할 트리거를 상당히 자유롭게 선택할 수 있습니다.

Riverbank에서 새 캐릭터로 시작할 때, 캐릭터가 깨어난 뒤 The Wounded Man과 대화하기 전까지의 짧은 구간은 의도적으로 기록하지 않습니다. 실제 런을 시작하기 전에 설정을 수정하거나, 'skip tutorial'을 선택하거나, 다른 옵션을 조정할 시간을 주기 위함입니다. The Wounded Man과 상호작용한 뒤 마지막 시작 대사에서 런 시간이 시작됩니다.

Zone-Transition-Start는 캐릭터가 미리 지정한 지역에 들어가는 즉시 활성화됩니다. 동적 런에서는 다른 지역에서 시작했더라도, 지정한 해당 지역에 들어갔을 때만 타이머와 추적이 시작됩니다.

게임이 길기 때문에 GameTimeWatcher를 개발했습니다. 이 간단한 프로그램은 Pause Game 메뉴 또는 소액결제 메뉴가 열려 있는 동안 LiveSplit의 Game Time을 일시정지하도록 지시합니다. 휴식이나 화면에서 벗어나 집중이 필요한 상황을 처리할 수 있도록 만든 기능입니다. 캐릭터를 조작할 수 있는 다른 메뉴에서는 타이머가 멈추지 않습니다. 게임 내 컷신 중에도 인벤토리에 접근할 수 있어 런 최적화에 사용할 수 있으므로 타이머가 계속 진행됩니다. 현재 타이머는 로딩 화면, 일시정지 메뉴, 소액결제 상점에서만 멈춥니다.

---

# 다운로드

다운로드는 [여기](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)에서 찾을 수 있습니다.

또는

이 GitHub 저장소의 **Releases** 섹션으로 이동하여 최신 버전을 다운로드하세요.

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

대부분의 사용자에게는 설치 프로그램을 권장합니다.

설치 프로그램을 사용하지 않으려는 사용자를 위해 포터블 ZIP이 제공될 수도 있습니다. 이 경우 PowerShell에서 `\Setup-UI[Configuration]\Build.ps1`을 실행하여 `RouteSetup.exe`를 생성해야 합니다.

---

# 빠른 시작

## 1. PoE2 Route AutoSplitter 설치

다음을 실행하세요.

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

설치 안내를 따르세요.

설치 후 다음을 실행합니다.

**PoE2 Route AutoSplitter**

경로 설정 애플리케이션이 시작됩니다.

---

## 2. 경로 선택

Setup 애플리케이션은 미리 만든 경로 목록을 제공합니다.

사용하려는 경로를 선택하세요.

예:

* Campaign Any%
* Campaign 100%
* 필수 보스만
* 탐험 경로
* Boss Rush 경로
* 탐험 + Boss Rush 결합 경로

**Custom Route**를 선택하여 직접 경로를 만들 수도 있습니다.

---

## 3. LiveSplit 설정 생성

경로를 선택한 뒤 Generate 버튼을 클릭하세요.

애플리케이션이 필요한 파일을 다음 디렉터리에 생성합니다.

`LiveSplit Target`

이 폴더에는 선택한 경로에 필요한 LiveSplit 파일이 들어 있습니다.

새 설정을 생성할 때마다 **LiveSplit Target**의 내용이 교체됩니다.

---

# LiveSplit 설정

LiveSplit에서 다음 두 가지를 설정해야 합니다.

1. 스플릿 파일 (`.lss`)
2. Scriptable Auto Splitter (`.asl`)

## 스플릿 파일 불러오기

생성된 **LiveSplit Target** 폴더에서 `.lss` 파일을 찾아 LiveSplit으로 엽니다.

LiveSplit에서 수동으로 불러오려면 다음을 사용합니다.

**File → Open Splits → From File**

생성된 `.lss` 파일을 선택하세요.

---

## Scriptable Auto Splitter 추가

오토스플리터 스크립트는 LiveSplit 레이아웃에 수동으로 추가해야 합니다.

LiveSplit에서:

1. LiveSplit을 마우스 오른쪽 버튼으로 클릭합니다.
2. **Edit Layout**을 선택합니다.
3. **+** 버튼을 클릭합니다.
4. 다음을 선택합니다.

   **Control → Scriptable Auto Splitter**

5. 새 **Scriptable Auto Splitter** 구성 요소를 선택합니다.
6. **LiveSplit Target** 폴더 안의 `.asl` 파일을 지정합니다.
7. 레이아웃을 저장합니다.

생성된 파일을 옮기거나 다른 ASL 파일을 사용하는 설정으로 바꿀 때만 이 경로를 변경하면 됩니다.

> PoE2 Route AutoSplitter는 개인 LiveSplit 레이아웃을 생성하거나 교체하지 **않습니다**.

레이아웃은 사용자가 직접 관리합니다.

---

# Boss Rush 설정

보스를 추적하는 경로는 포함된 **BossWatcher** 프로그램을 사용합니다.

BossWatcher는 게임에서 보스 이름을 읽고 보스 이벤트를 오토스플리터로 전달합니다.

선택한 경로에 BossWatcher가 필요하면 PoE2 Route Setup 안에서 다음 버튼을 사용하세요.

**Start BossWatcher**

콘솔 창이 열립니다.

일반 사용 시 BossWatcher는 다음과 같은 유용한 이벤트만 표시합니다.

* 보스 조우
* 보스 처치
* 전투 시간

예:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

런 중 BossWatcher 콘솔을 조작할 필요는 없습니다.

스피드런 동안 열어 두세요.

---

# 탐험 경로

탐험 경로는 캐릭터가 Path of Exile 2의 특정 지역에 들어가는 것을 감지합니다.

탐험 전용 경로에서는 BossWatcher가 **필요하지 않습니다**.

오토스플리터가 Path of Exile 2의 지역 전환 정보를 자동으로 읽습니다.

---

# 탐험 + Boss Rush 결합

결합 경로는 다음을 모두 추적합니다.

* 지역 완료
* 보스 처치

이 경로에서는:

1. 생성된 `.lss`를 불러옵니다.
2. Scriptable Auto Splitter가 생성된 `.asl`을 가리키도록 합니다.
3. PoE2 Route Setup에서 BossWatcher를 시작합니다.
4. 런을 시작합니다.

지역 목표와 보스 목표가 같은 경로에서 함께 처리됩니다.

---

# 사용자 지정 경로

PoE2 Route Setup에서 **Custom Route**를 선택하면 직접 경로를 만들 수 있습니다.

다음을 포함할 수 있습니다.

* 지역
* 보스
* 지역과 보스 모두

원하는 목표를 추가하고 원하는 순서로 정렬하세요.

완료되면 설정을 생성합니다.

애플리케이션은 **LiveSplit Target** 안에 다음을 생성합니다.

* `.lss`
* `.asl`
* 경로 설정

위와 동일한 LiveSplit 절차로 이 파일들을 불러오세요.

---

# Trials

Trial of the Sekhemas 및 Temple of Chaos용입니다.

시작 조건은 실제 Trial에 처음 들어가는 순간입니다. 준비를 하는 로비는 추적하지 않습니다.

종료 조건은 두 가지가 있습니다.

1. Trial에서 어디까지 진행할지 선택합니다. 지정한 깊이의 보스를 처치하면 Trial이 성공적으로 종료됩니다. Trial을 완료하지 못하면 실패한 런으로 간주되며 수동 재시작이 필요합니다.

2. Trial에서 나가면 완료로 처리합니다. Trial 경기장을 나가는 시점을 종료 조건으로 사용하려는 경우에 적합합니다. 이 경우 전리품, 캐시, 상인, Ascendancy 선택도 런 시간에 포함됩니다.

---

# Vaal Ruins

전환 처리 때문에 로비는 경계 지역으로 취급됩니다. 따라서 Map에서 콘솔 방으로 들어가는 것은 해당 Map의 하위 지역에 들어가는 것이 아니라 Map을 나간 것으로 처리됩니다.

Vaal Ruins는 아직 개발 중입니다.

---

# Maps

Hideout 또는 다른 Map 허브에 있는 동안 Map 준비 시간은 기록하지 않습니다. Map에 들어가면 타이머가 자동으로 시작되고, 지역 보스를 처치한 뒤 처음 나갈 때 스플릿합니다. 보스를 처치하기 전에 Map에서 나가면 타이머는 계속 진행됩니다. 따라서 보스를 빠르게 처치하고 Map을 나간 뒤 같은 Map에 다시 들어가 추가 콘텐츠를 진행하면서 타이머를 멈춘 상태로 유지할 수 있습니다. (대체 정책은 아래 참고.)

Map 런에는 여러 종료 조건이 있습니다.

* 고정된 Map 횟수
* 첫 사망까지 (Deathless Run)
* 수동 종료
* 특정 Pinnacle 보스 처치

사망 추적에는 세 가지 옵션이 있습니다.
* 사망 추적 안 함
* 첫 사망만
* 사망 횟수 추적

첫 사망 또는 사망 추적을 선택하면 게임에 표시된 캐릭터 이름을 정확히 입력해야 합니다. 클라이언트 로그를 읽어 캐릭터의 사망을 식별하기 때문입니다.

일시정지 정책은 두 가지가 있습니다.

* 보스 처치를 Map 완료 이벤트로 사용하고, 보스 처치 후 처음 나갈 때 스플릿이 종료됩니다. PoE2의 Map 완료 정책과 유사합니다.
* 대체 정책: 타이머는 로딩 화면, 수동 일시정지, 소액결제 메뉴(활성화한 경우)에서만 멈춥니다. Map 준비, 인벤토리 관리, 전리품 확인 등 그 외 모든 시간에는 계속 진행됩니다.

# 경로 전환

다른 경로로 전환하려면:

1. PoE2 Route Setup을 엽니다.
2. 새 경로를 선택합니다.
3. 설정을 다시 생성합니다.
4. 새 `.lss`를 LiveSplit에서 엽니다.
5. Scriptable Auto Splitter가 **LiveSplit Target** 안의 `.asl`을 가리키는지 확인합니다.
6. 새 경로에 보스 감지가 필요하면 BossWatcher를 시작합니다.

이전 **LiveSplit Target**의 내용은 교체됩니다.

---

# 런 시작

설정이 끝나면:

1. Path of Exile 2를 실행합니다.
2. LiveSplit을 실행합니다.
3. 경로의 `.lss`를 불러옵니다.
4. Scriptable Auto Splitter가 올바른 `.asl`을 사용하는지 확인합니다.
5. 경로에 보스가 포함되어 있으면 BossWatcher를 시작합니다.
6. 런을 시작합니다.

오토스플리터가 설정된 경로 목표를 자동으로 처리합니다.

---

# 업데이트

새 버전이 출시되면:

1. **GitHub Releases**에서 최신 설치 프로그램을 다운로드합니다.
2. 설치 프로그램을 실행합니다.
3. PoE2 Route Setup을 엽니다.
4. 경로를 다시 생성합니다.

개인 LiveSplit 레이아웃은 교체할 필요가 없습니다.

---

# 문제 해결

## 보스에서 스플릿되지 않음

다음을 확인하세요.

* BossWatcher가 실행 중입니다.
* PoE2 Route Setup에서 BossWatcher를 시작했습니다.
* 선택한 경로에 실제로 보스 목표가 포함되어 있습니다.
* LiveSplit의 Scriptable Auto Splitter가 생성된 `.asl`을 가리킵니다.

---

## 지역에서 스플릿되지 않음

다음을 확인하세요.

* Path of Exile 2가 실행 중입니다.
* LiveSplit의 Scriptable Auto Splitter가 올바른 `.asl`을 가리킵니다.
* 올바른 탐험 경로를 생성했습니다.
* 올바른 `.lss`가 로드되어 있습니다.

---

## LiveSplit이 잘못된 splits를 엶

다음에서 `.lss`를 직접 여세요.

`LiveSplit Target`

또는:

**File → Open Splits → From File**

---

## 경로를 바꾼 뒤 작동하지 않음

새 경로를 다시 생성하고 다음을 확인하세요.

* 올바른 `.lss`가 로드되어 있습니다.
* Scriptable Auto Splitter가 **LiveSplit Target** 안의 현재 `.asl`을 가리킵니다.

---

## BossWatcher에서 오류가 표시됨

BossWatcher를 닫고 PoE2 Route Setup의 **Start BossWatcher** 버튼으로 다시 시작하세요.

문제가 계속되면 문제 보고 시 표시된 오류를 함께 보내 주세요.

---
## BossWatcher가 너무 일찍 스플릿하거나 플레이어 사망 시 스플릿함

BossWatcher는 보스 체력 바가 화면에서 사라지는 시점을 기록합니다. 여러 이유로 체력 바가 사라질 수 있으므로 스플릿이 올바른지는 사용자가 판단해야 합니다. 기본적으로 보스가 죽었다고 가정하고 스플릿합니다. 보스를 완료하지 않았는데 스플릿이 발생했다면 split undo를 사용해 이전 상태로 되돌리고 현재 시간에서 다시 보스에 도전할 수 있습니다. Split undo 단축키는 LiveSplit 설정에 있습니다.

---

# LiveSplit용 생성 파일

선택한 경로에 따라 **LiveSplit Target**에는 다음이 포함될 수 있습니다.

### `.lss`

LiveSplit 스플릿 목록입니다.

### `.asl`

LiveSplit의 Scriptable Auto Splitter 구성 요소가 사용하는 오토스플리터 스크립트입니다.

### 경로/설정 파일

선택한 경로에 어떤 지역 및/또는 보스가 포함되는지 오토스플리터에 알려 줍니다.

### 보스 이벤트 파일

BossWatcher와 보스 지원 오토스플리터가 사용합니다.

무엇을 변경하는지 정확히 알지 못한다면 이 파일들을 수동으로 수정하지 마세요.

일반적으로는 **PoE2 Route Setup**을 통해 생성하세요.

---

# 중요

PoE2 Route AutoSplitter는 개인 LiveSplit 레이아웃을 제어하거나 교체하지 **않습니다**.

다음 항목은 사용자가 직접 관리합니다.

* 타이머 모양
* 스플릿 색상
* 글꼴
* 창 크기
* 비교 설정
* 기타 LiveSplit 구성 요소

PoE2 Route AutoSplitter는 경로 스플릿과 오토스플리터 설정만 제공합니다.

---

# 문제 보고

문제를 보고할 때 다음을 포함해 주세요.

* PoE2 Route AutoSplitter 버전
* 사용 중인 경로/모드
* BossWatcher 실행 여부
* 예상한 동작
* 실제로 발생한 동작
* PoE2 Route Setup, BossWatcher 또는 LiveSplit에서 표시된 오류 메시지

이 정보가 있으면 문제를 재현하고 수정하기가 훨씬 쉬워집니다.

---

# 패키지 검증 및 진단

릴리스/런타임 파일 검증용 SHA-256 매니페스트는 다음 위치에 저장됩니다.

`3 - verification files`

설정 검증 매니페스트, 각 런의 SHA-256 매니페스트, 감사 로그, 읽기 쉬운 런 요약도 이 폴더에 저장됩니다. 이 파일들은 `LiveSplit Target` 밖에 유지되므로 새 경로를 생성해도 이전 런 감사 파일은 삭제되지 않습니다.

SetupUI, BossWatcher, GameTimeWatcher의 진단 로그는 다음 위치에 통합됩니다.

`4-README's_and_Diagnostics\Diagnostics`

진단 PNG 캡처는 다음 위치에 저장됩니다.

`4-README's_and_Diagnostics\Diagnostics\images`

---

# 현재 주요 버전

**PoE2 Route AutoSplitter 3.x**

Version 3은 SetupUI 및 게임 언어의 다국어 지원, 검증된 보스/지역 이름 현지화, Campaign/Trials/Vaal Ruins/Maps 정책 확장, 진단 및 검증 파일 통합, 그리고 표준 16:9·울트라와이드·슈퍼 울트라와이드 게임 클라이언트에 대응하는 높이 기준의 BossWatcher 적응형 캡처 지오메트리를 추가합니다.
