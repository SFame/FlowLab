# ===============================================================
# IronPython 버전: 3.4.2 (3.4.2.1000)
# ===============================================================

# ==================== 사용 가능한 Python 모듈 ===================
# 기본 시스템/유틸리티: sys, time, gc, atexit, itertools, marshal, signal
# 수학/계산: math, cmath, _random, _heapq, _bisect
# 문자열/텍스트 처리: re, _string, _sre, _struct, _csv
# 데이터 구조: array, _collections
# 파일/IO: _io, zipimport, _bz2
# 네트워킹: _socket, _ssl, _overlapped
# 윈도우 특화: msvcrt, winreg, winsound, _winapi, nt
# ===============================================================

# ===================== .NET Framework 접근 =====================
# IronPython을 통해 .NET Framework의 모든 라이브러리에 접근 가능합니다
# 기본 참조: System, System.Net

# 추가 참조가 필요할 경우:
# add_reference('필요한_어셈블리')
# from 필요한_네임스페이스 import 필요한_클래스

# 예시:
# add_reference('System')
# add_reference('System.Threading')
# add_reference('System.Net')
#
# import System
# from System.Net import WebClient
# from System.Threading import Thread, ThreadStart

# 주요 유용한 .NET 네임스페이스:
# - System: 기본 클래스, 데이터 타입, 유틸리티
# - System.IO: 파일 및 디렉토리 작업
# - System.Net: 네트워크 통신, HTTP 요청
# - System.Threading: 스레드, 타이머, 동기화
# - System.Collections: 컬렉션, 리스트, 딕셔너리
# - System.Text: 문자열 처리, 인코딩
# ===============================================================



# <<노드 속성>>

# 노드의 이름 정의
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
name: str = "Debug"

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_list: list = ['in 1', 'in 2', 'in 3', 'in 4']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['out 1', 'out 2', 'out 3', 'in 4']

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. input_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_types: list = [bool, int, float, str]

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. output_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_types: list = [bool, int, float, str]

# True일 경우, 이 노드의 메서드를 비동기적으로 실행할 수 있습니다(하지만 terminate()는 언제나 동기적으로 실행됩니다)
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
is_async: bool = False



# <<노드 컨트롤러>>

# ====================== WARNING ======================
# 다음 변수들을 수정하지 마세요
# 이 변수들은 시스템에 의해 자동으로 초기화 됩니다

# 출력 포트를 제어하는 객체
# 사용 가능한 API: def apply(self, outputs: list) -> None:
# 입력으로 output_types 배열과 일치하는 순서로 해당 타입 값의 리스트를 제공해야 합니다. ※이 리스트의 길이는 출력 포트 수와 일치해야 합니다
output_applier: OutputApplier = None

# 프린터 객체
# 사용 가능한 API: def print(self, value: str) -> None:
# 노드의 디스플레이에 문자열 정보를 표시하는 데 사용됩니다
printer: Printer = None
# =====================================================



# <<노드 생명주기 메서드>>

def init(inputs: list) -> None:
    input_values = [f"{i}: {value}" for i, value in enumerate(inputs)]
    printer.print(f"Inputs - {', '.join(input_values)}")

    output_applier.apply([None if inputs[0] is None else not inputs[0], None if inputs[1] is None else 10, None if inputs[2] is None else 10.0, None if inputs[3] is None else "각"])


def terminate() -> None:
    """
    노드가 삭제되거나 실행취소/다시실행 작업 중에 호출되는 정리 함수입니다
    정리가 필요한 모든 리소스를 해제하세요
    """
    pass

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    printer.print(f"index: {index}, state: {state}, is_changed: {is_changed}, before_state: {before_state}")
    # 입력 포트의 상태를 출력 포트에 전달
    # 0번은 반전, 1, 2번은 * 10
    # output_applier.apply([not inputs[0], inputs[1] * 10, inputs[2] * 10])

    if index == 0:
        output_applier.apply_at(0, None if state is None else not state)
    elif index == 1:
        output_applier.apply_to('out 2', None if state is None else state * 10)
    elif index == 2:
        output_applier.apply_at(2, None if state is None else state * 10)
    elif index == 3:
        output_applier.apply_at(3, None if state is None else state + " desu.")