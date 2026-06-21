<div align="center">

# FlowLab

[![Unity](https://img.shields.io/badge/Unity-6000.2.6f2-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-.NET%20Framework-512BD4?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![IronPython](https://img.shields.io/badge/IronPython-3.4.2-3776AB?logo=python)](https://ironpython.net/)

[🚀 시작하기](#-시작하기) · [⌨️ 단축키](#️-키보드-단축키) · [📦 노드 가이드](#-노드-가이드) · [🐍 스크립팅](#-scriptingnode) · [⚙️ 설정](#️-설정)

🌐 [English](README.md)

</div>

---

## 🚀 시작하기

### 기본 워크플로우

```
1. 노드 배치  →  2. 포트 연결  →  3. 실행 확인
```

### 1. 노드 배치하기

`Tab` 키를 눌러 **노드 팔레트**를 엽니다.

원하는 노드를 드래그하여 작업 공간에 배치합니다.

### 2. 노드 연결하기

노드의 **출력 포트**(오른쪽)에서 드래그를 시작해서 다른 노드의 **입력 포트**(왼쪽)에 연결합니다.

> 💡 **Tip:** 같은 타입의 포트끼리만 연결됩니다. (포트 색상으로 타입 구분)

### 3. 실행 확인

연결이 완료되면 데이터가 자동으로 흐릅니다. Trigger 노드를 클릭하거나 입력값을 변경하면 결과가 실시간으로 반영됩니다.

---

## ⌨️ 키보드 단축키

### 기본 조작

| 기능 | 단축키 | 설명 | 설정 변경 |
|------|--------|------|------|
| **노드 팔레트 열기** | `Tab` | 노드 목록 표시 | ✓ |
| **세이브 패널 열기** | `S` | 저장/불러오기 | ✓ |
| **콘솔 열기** | `` ` `` | 디버그 콘솔 | ✓ |
| **미니맵 열기/닫기** | `M` | 전체 맵 미리보기 | ✓ |

### 편집

| 기능 | 단축키 | 설정 변경 |
|------|--------|:--------:|
| **실행취소** | `Ctrl + Z` | ✓ |
| **다시실행** | `Ctrl + Shift + Z` | ✓ |
| **복사** | `Ctrl + C` | ✓ |
| **붙여넣기** | `Ctrl + V` | ✓ |
| **잘라내기** | `Ctrl + X` | ✓ |
| **전체선택** | `Ctrl + A` | ✓ |
| **선택 삭제** | `Delete` | ✓ |
| **연결 해제** | `Backspace` | ✓ |

### 뷰 조작

| 기능 | 조작 |
|------|------|
| **영역 선택** | 좌클릭 드래그 |
| **맵 이동** | 우클릭 드래그 / 미니맵 클릭 |
| **수직 이동** | 마우스 휠 |
| **수평 이동** | `Shift + 휠` |
| **줌 인** | `Ctrl + 휠 ↑` |
| **줌 아웃** | `Ctrl + 휠 ↓` |

### 스냅 모드

| 기능 | 단축키 | 설정 변경 |
|------|--------|:--------:|
| **스냅 모드 토글** | `Q` | ✓ |

> 스냅 모드 활성화 시, 라인의 엣지가 자동으로 직각 정렬됩니다.

---

## 📦 노드 가이드

### 데이터 타입

FlowLab은 5가지 데이터 타입을 지원하며, 포트 색상으로 구분됩니다.

| 타입 | 설명 | 예시 |
|------|------|------|
| ![Bool](https://img.shields.io/badge/Bool-blue) | 참/거짓 | `true`, `false` |
| ![Int](https://img.shields.io/badge/Int-green) | 정수 | `0`, `42`, `-100` |
| ![Float](https://img.shields.io/badge/Float-orange) | 실수 | `3.14`, `0.5` |
| ![String](https://img.shields.io/badge/String-pink) | 문자열 | `"Hello"` |
| ![Pulse](https://img.shields.io/badge/Pulse-white) | 실행 신호 | 이벤트 트리거 |

### 노드 카테고리

#### Logic (10개)
논리 연산을 수행합니다.

| 노드 | 설명 |
|------|------|
| AND | 모든 입력이 true일 때 true |
| OR | 하나라도 true면 true |
| NAND | AND의 반전 |
| NOR | OR의 반전 |
| XOR | 입력이 서로 다르면 true |
| XNOR | 입력이 서로 같으면 true |
| NOT | 입력 반전 |
| All | 모든 입력이 true인지 확인 |
| Any | 하나라도 true인지 확인 |
| Comparator | 두 입력 비교 |

#### Memory (5개)
상태를 저장합니다.

| 노드 | 설명 |
|------|------|
| SR Latch | Set/Reset 래치 |
| D Flip-Flop | 클럭 엣지에 데이터 저장 |
| T Flip-Flop | 클럭 엣지에 토글 |
| JK Flip-Flop | JK 플립플롭 |
| Counter | 펄스 카운트 |

#### Flow (5개)
프로그램 흐름을 제어합니다.

| 노드 | 설명 |
|------|------|
| If | 조건에 따라 분기 |
| While | 조건이 참인 동안 반복 |
| Branch | 다중 분기 |
| Select | 값에 따라 선택 |
| Sequence | 출력을 순서대로 실행 |

#### I/O (8개)
사용자 입출력을 처리합니다.

| 노드 | 설명 |
|------|------|
| Trigger | 클릭 시 Pulse 신호 발생 |
| Input Field | 텍스트/숫자 입력 |
| Input Switch | 선택형 입력 |
| On/Off Switch | 토글 스위치 |
| Key Input | 키보드 입력 |
| Display | 값 표시 |
| 7-Segment Display | 숫자 세그먼트 표시 |
| Binary Display | 이진수 표시 |

#### Signal (13개)
신호를 생성, 분배, 병합합니다.

| 노드 | 설명 |
|------|------|
| Split | 하나의 신호를 여러 출력으로 분배 |
| Merger | 여러 신호를 하나로 병합 |
| Switch | 제어 입력으로 신호 경로 전환 |
| Sender | 짝지어진 수신기로 신호 전송 |
| Signal Detector | 신호 변화 감지 |
| Edge Detector | 상승/하강 엣지 감지 |
| One Hot | 단일 출력만 활성화 |
| One Shot | 단일 펄스 발생 |
| Blink | 주기적 신호 생성 |
| Timer | 설정 시간 후 출력 |
| Delay | 신호 지연 |
| Debouncer | 신호 노이즈 제거 |
| Frequency Meter | 신호 주파수 측정 |

#### Math (31개)
수학 연산을 수행합니다.

| 노드 | 설명 |
|------|------|
| Add, Subtract, Multiply, Divide | 사칙연산 |
| Modulo | 나머지 |
| Pow, Square Root | 거듭제곱 / 제곱근 |
| Absolute, Round | 절댓값 / 반올림 |
| MinMax, Clamp | 범위 연산 |
| Average, Standard Deviation | 통계 |
| Sin, Cos, Tan | 삼각함수 |
| Asin, Acos, Atan, Atan2 | 역삼각함수 |
| Sinh, Cosh, Tanh | 쌍곡선함수 |
| Lerp | 선형 보간 |
| Equal, Numeric Comparator | 비교 |
| True Count | true 입력 개수 카운트 |
| Binary Encoder, Binary Decoder | 이진 변환 |
| Random | 난수 생성 |
| Formula | 사용자 정의 수식 계산 |

#### Util (11개)
유틸리티 기능을 제공합니다.

| 노드 | 설명 |
|------|------|
| String Length | 문자열 길이 |
| String Replace | 문자열 치환 |
| String Concat | 문자열 결합 |
| String Contain | 부분 문자열 확인 |
| String Split | 문자열 분리 |
| To Upper / To Lower | 대소문자 변환 |
| Trim | 공백 제거 |
| Type Converter | 타입 변환 |
| Is Null | null 확인 |
| Null Filter | null 신호 필터링 |

#### Advanced (3개)
고급 확장 기능입니다.

| 노드 | 설명 |
|------|------|
| Scripting | Python 스크립트 실행 |
| Classed | 회로를 모듈로 캡슐화 |
| Console | 콘솔 명령 실행 |

---

## 🐍 ScriptingNode

Python 스크립트로 커스텀 노드를 만들 수 있습니다.

### 템플릿 생성

ScriptingNode를 **우클릭** → 컨텍스트 메뉴에서 **Create Template** 클릭

기본 뼈대가 포함된 Python 스크립트 파일이 생성됩니다.

### 기본 템플릿

```python
# 노드 이름
name: str = "My Custom Node"

# 입력 포트 설정
input_list: list = ['input1', 'input2']
input_types: list = [float, float]

# 출력 포트 설정
output_list: list = ['result']
output_types: list = [float]

# 비동기 모드 (오래 걸리는 작업 시 True)
is_async: bool = False

# 시스템 주입 객체 (수정 금지)
output_applier: OutputApplier = None
printer: Printer = None

# 생명주기 함수
def init(inputs: list) -> None:
    """노드 초기화 시 호출"""
    pass

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """입력 변경 시 호출"""
    a = inputs[0]
    b = inputs[1]
    
    if a is not None and b is not None:
        output_applier.apply([a + b])

def terminate() -> None:
    """노드 삭제 시 호출"""
    pass
```

### 사용 가능한 타입

| 타입 | Python | 예시 |
|------|--------|------|
| Bool | `bool` | `True`, `False` |
| Int | `int` | `42` |
| Float | `float` | `3.14` |
| String | `str` | `"Hello"` |
| Pulse | `Pulse` | `Pulse()` |

### 출력 제어 API

```python
# 모든 출력 한번에 설정
output_applier.apply([value1, value2, value3])

# 특정 인덱스 출력 설정
output_applier.apply_at(0, value)

# 이름으로 출력 설정
output_applier.apply_to('result', value)

# 신호 소실 (None 전달)
output_applier.apply([True, None, 3.14])
```

### Python 표준 라이브러리

Python 표준 라이브러리 대부분을 사용할 수 있습니다.

```python
import json
import datetime
import collections

data = {"name": "FlowLab", "version": 1.0}
json_str = json.dumps(data)
parsed = json.loads(json_str)
```

### .NET 라이브러리 사용

```python
# 어셈블리 참조 추가
add_reference('System')

# 네임스페이스 import
from System.Net import WebClient
from System.Threading import Thread

# HTTP 요청 예시
client = WebClient()
response = client.DownloadString("https://api.example.com/data")
```

### 예제: HTTP API 호출

```python
name: str = "API Caller"
input_list: list = ['url', 'trigger']
input_types: list = [str, Pulse]
output_list: list = ['response', 'status']
output_types: list = [str, bool]
is_async: bool = True  # 네트워크 요청은 비동기로!

output_applier: OutputApplier = None
printer: Printer = None

add_reference('System')
from System.Net import WebClient

def state_update(inputs, index, state, before_state, is_changed):
    if index == 1 and inputs[0] is not None:  # trigger 입력 시
        try:
            client = WebClient()
            response = client.DownloadString(inputs[0])
            output_applier.apply([response, True])
            printer.print("Success!")
        except Exception as e:
            output_applier.apply([str(e), False])
            printer.print("Error: " + str(e))
```

---

## 📦 ClassedNode

복잡한 회로를 하나의 재사용 가능한 노드로 만들 수 있습니다.

### 사용 방법

1. **회로 설계**: 일반 노드들로 원하는 기능 구현
2. **External I/O 배치**: 외부 입출력 포트 정의
3. **ClassedNode로 변환**: 회로 전체를 하나의 노드로 캡슐화
4. **저장/공유**: `.lcm` 파일로 Export

### 저장 및 불러오기

| 작업 | 방법 |
|------|------|
| **저장** | Save/Load 패널 → Export → 세이브 선택 → 파일명 입력 |
| **불러오기** | Save/Load 패널 → Import → .lcm 파일 선택 |

> 💡 팀원과 `.lcm` 파일을 공유하면 동일한 커스텀 노드를 사용할 수 있습니다.

---

## ⚙️ 설정

### 설정 열기

화면 좌상단에 마우스를 가져가면 툴바가 나타납니다.

`?` 버튼 클릭 → `Option` 선택

### 시뮬레이션 속도

| 모드 | 설명 |
|------|------|
| **Frame** | 프레임 단위로 신호 전파 |
| **Fixed Time** | 지정된 시간 간격으로 전파 (0.01 ~ 10초) |
| **⚡ Immediately** | 즉시 전파 (최고 성능) |

#### Immediately 모드

체크 시 **Max Iterations Per Frame** 설정이 나타납니다.

이 값은 피드백 루프 감지 임계치입니다. 한 프레임 내에서 같은 연결을 이 횟수 이상 통과하면 자동으로 다음 프레임으로 지연됩니다.

> 💡 일반적인 회로에서는 기본값(5)을 권장합니다.

### 키 매핑

설정 화면에서 단축키를 원하는 키로 변경할 수 있습니다.

변경 가능한 항목:
- Undo / Redo
- Copy / Cut / Paste
- ToggleSnapMode
- OpenPalette
- OpenSaveLoadPanel
- SelectAll / SelectDelete
