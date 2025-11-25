# ===============================================================
# IronPython 버전: 3.4.2 (3.4.2.1000)
# ===============================================================

# ==================== 사용 가능한 Python 모듈 ===================
# Python 표준 라이브러리의 대부분의 모듈을 사용할 수 있습니다.
# 예: json, collections, datetime, pathlib, threading, asyncio 등
# ===============================================================

# ===================== .NET Framework 접근 =====================
# IronPython을 통해 .NET Framework의 모든 라이브러리에 접근 가능합니다
# 기본 참조: System, System.Net

# 추가 참조가 필요할 경우:
# add_reference('필요_어셈블리')
# from 필요_네임스페이스 import 필요_클래스

# 예시:
# add_reference('System')
#
# import System
# from System.Net import WebClient
# from System.Threading import Thread, ThreadStart

# ※주의사항:
# add_reference('System.Threading')과 같은 형태는 예외가 발생할 수 있습니다
# 따라서 add_reference()의 인자로는 최상위 어셈블리 이름을 사용하는 것을 권장합니다

# 주요 유용한 .NET 네임스페이스:
# - System: 기본 클래스, 데이터 타입, 유틸리티
# - System.IO: 파일 및 디렉토리 작업
# - System.Net: 네트워크 통신, HTTP 요청
# - System.Threading: 스레드, 타이머, 동기화
# - System.Collections: 컬렉션, 리스트, 딕셔너리
# - System.Text: 문자열 처리, 인코딩
# ===============================================================



# ========================== 타입 가이드 =========================
# input_types와 output_types 리스트에 사용할 수 있는 타입들은 다음과 같습니다.

# 1. bool
#    - 참(True) 또는 거짓(False)의 논리적 '상태(State)'를 나타냅니다.
#    - 예: output_applier.apply([True, True, False])

# 2. int
#    - 정수 값을 나타냅니다.
#    - 예: output_applier.apply([10, 20, 0])

# 3. float
#    - 소수점을 포함한 숫자 값을 나타냅니다.
#    - 예: output_applier.apply([10.9, 0.12, 0.0])

# 4. str
#    - 문자열(텍스트) 값을 나타냅니다.
#    - 예: output_applier.apply(['Hello', "World"])

# 5. Pulse
#    - 노드의 동작을 유발하는 '실행 신호(Event)'를 나타내는 타입입니다.
#      '상태(State)'를 나타내는 bool과 구분되어 사용됩니다.
#    - ▶ Exec, ▶ Then, ▶ Loop 등과 같은 실행 포트에 사용됩니다.
#    - [출력] Pulse 타입의 출력 포트에서 신호를 발생시키려면, Pulse() 인스턴스를 생성하여 전달해야 합니다.
#      # 예: output_applier.apply([Pulse(), Pulse()])

# ※주의사항:
# 스크립트 내에서 직접 'class Pulse:' 와 같이 클래스를 선언하지 마십시오.
# Pulse 클래스는 시스템에 의해 미리 정의되어 제공되므로,
# 직접 선언하면 스크립트가 올바르게 작동하지 않거나 예상치 못한 오류가 발생할 수 있습니다.
# ===============================================================



# Scripting Node는 아래의 멤버를 반드시 포함해야 합니다
# ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓

# <<노드 속성>>

# 노드의 이름 정의
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
name: str = 'Scripting Node'

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_list: list = ['in 1', 'in 2']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['out 1']

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. input_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, str, Pulse
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_types: list = [bool, bool]

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. output_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, Pulse
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_types: list = [bool]

# True일 경우, 이 노드의 메서드를 비동기적으로 실행할 수 있습니다(하지만 terminate()는 언제나 동기적으로 실행됩니다)
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
is_async: bool = False



# <<노드 컨트롤러>>

# ====================== WARNING ======================
# 다음 변수들을 수정하지 마세요
# 이 변수들은 시스템에 의해 자동으로 초기화 됩니다

# 출력 포트를 제어하는 객체
# <사용 가능한 API>
#   output_applier.apply(values: list) -> None:
#   output_applier.apply_at(index: int, value) -> None:
#   output_applier.apply_to(name: str, value) -> None:
# apply: 전체 출력 일괄 업데이트. 입력으로 output_types 배열과 일치하는 순서로 해당 타입 값의 리스트를 제공해야 합니다. ※이 리스트의 길이는 출력 포트 수와 일치해야 합니다
# apply_at: index 위치의 출력 포트에 값을 할당합니다
# apply_to: name 의 이름을 가진 출력 포트에 값을 할당합니다. ※출력 포트의 이름에 중복이 있는 경우 사용할 수 없습니다

# ※모든 API의 value 입력에는 None을 할당할 수 있습니다. 이를 통해 해당 네트워크의 다음과 같이 신호를 소실시킬 수 있습니다
# output_applier.apply([True, None, 3.14])  # 2번째 포트만 신호 소실
# output_applier.apply_at(1, None)          # 1번 포트 신호 소실  
# output_applier.apply_to('out 2', None)    # 'out 2' 포트 신호 소실

# Pulse 타입의 신호를 할당하려면 Pulse 인스턴스를 생성하여 사용하십시오
# output_applier.apply([Pulse(), True, False])
output_applier: OutputApplier = None

# 프린터 객체
# <사용 가능한 API> 
#   printer.print(value: str) -> None:
# 노드의 디스플레이에 문자열 정보를 표시하는 데 사용됩니다
printer: Printer = None
# =====================================================



# <<유틸리티>>

# JSON 유틸리티 객체 (Deprecated)
# 일반적인 사용에는 'import json'을 권장합니다
# <사용 가능한 API>
#   json_util.serialize(data, pretty: bool=False) -> str:
#   json_util.deserialize(json_text: str) -> object:
#   json_util.try_serialize(data, pretty: bool=False) -> (bool, str):
#   json_util.try_deserialize(json_text: str) -> (bool, object):
#   json_util.is_valid(json_text: str) -> bool:
json_util: JsonUtil = JsonUtil()



# <<노드 생명주기 메서드>>

def init(inputs: list) -> None:
    """
    노드가 새로 생성되거나 실행취소/다시실행 작업 중에 호출되는 초기화 함수입니다
    처음 출력 포트들의 상태를 설정하는 용도로 적합합니다
    요소의 값을 변경하지 말고 읽기만 하십시오
    간결하고 효율적으로 작성하세요

    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 리스트.
            input_type 순서와 일치하는 타입의 값이 입력됩니다.
            ※이 리스트에는 None이 포함될 수 있습니다. 이는 연결 해제된 포트나 신호가 소실된 네트워크에서 발생할 수 있습니다
    """
    pass

def terminate() -> None:
    """
    노드가 삭제되거나 실행취소/다시실행 작업 중에 호출되는 정리 함수입니다
    정리가 필요한 모든 리소스를 해제하세요
    """
    pass

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    입력 포트에서 신호가 감지될 때마다 호출됩니다.
    모든 매개변수를 사용할 필요는 없습니다. 대부분의 경우 inputs만 사용하면 됩니다
    하지만 "신호가 들어온 포트"의 종류, 해당 포트의 상태, 이전 상태와의 차이를 알고 싶다면 나머지 매개변수도 사용하세요
    
    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 리스트. 
            input_type 순서와 일치하는 타입의 값이 입력됩니다. 
            ※이 리스트에는 None이 포함될 수 있습니다. 이는 연결 해제된 포트나 신호가 소실된 네트워크에서 발생할 수 있습니다

        index (int): 방금 변경된 입력 포트의 인덱스

        state (input_type): 변경된 포트의 새 상태 값
            설정된 input_types에 따라 입력됩니다
            ※연결 해제 또는 연결된 해당 네트워크에서의 신호 소실에 의한 호출인 경우 None이 입력될 수 있습니다

        before_state (input_type): 변경된 포트의 이전 상태 값
            설정된 input_types에 따라 입력됩니다
            ※이전 상태가 연결이 해제된 상태였거나, 신호가 소실된 네트워크 였던 경우 None이 입력될 수 있습니다

        is_changed (bool): 변경된 포트의 상태가 이전과 다른지 여부를 나타내는 플래그
    """
    pass