# ===============================================================
# Python 버전: 3.4.2 (3.4.2.1000)
# ===============================================================

# ====================== 사용 가능 모듈 =========================
# 기본 시스템/유틸리티: sys, time, gc, atexit, itertools, marshal, signal
# 수학/계산: math, cmath, _random, _heapq, _bisect
# 문자열/텍스트 처리: re, _string, _sre, _struct, _csv
# 데이터 구조: array, _collections
# 파일/IO: _io, zipimport, _bz2
# 네트워킹: _socket, _ssl, _overlapped
# .NET 통합: clr
# 윈도우 특화: msvcrt, winreg, winsound, _winapi, nt
# ===============================================================

# 노드의 이름 정의
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 초기화 이후 변경은 효과가 없습니다
name: str = "Scripting Node"

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 초기화 이후 변경은 효과가 없습니다
input_list: list = ['in 1', 'in 2']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 초기화 이후 변경은 효과가 없습니다
output_list: list = ['out 1']

# True일 경우, 이 노드의 메서드를 비동기적으로 실행할 수 있습니다(하지만 terminate()는 언제나 동기적으로 실행됩니다)
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 초기화 이후 변경은 효과가 없습니다
is_async: bool = False

# 초기화 후 state_update가 자동으로 호출되는지 제어
# True일 경우, 시스템은 init() 후 한 번 state_update를 호출합니다
# (index=-1, state=False, is_changed=False) 파라미터와 함께 호출됩니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 초기화 이후 변경은 효과가 없습니다
auto_state_update_after_init: bool = False


# ====================== WARNING ======================
# 다음 변수들을 수정하지 마세요
# 이 변수들은 시스템에 의해 자동으로 초기화 됩니다

# 출력 포트를 제어하는 객체
# 사용 가능한 API: def apply(self, outputs: list) -> None:
# 입력으로 불리언 리스트를 제공해야 합니다. 이 리스트의 길이는 출력 포트 수와 일치해야 합니다
output_applier: OutputApplier = None

# 프린터 객체
# 사용 가능한 API: def print(self, value: str) -> None:
# 노드의 디스플레이에 문자열 정보를 표시하는 데 사용됩니다
printer: Printer = None
# =====================================================


def init(inputs: list) -> None:
    """
    노드가 새로 생성되거나 실행취소/다시실행 작업 중에 호출되는 초기화 함수입니다
    간결하고 효율적으로 작성하세요

    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 불리언 리스트
    """
    pass

def terminate() -> None:
    """
    노드가 삭제되거나 실행취소/다시실행 작업 중에 호출되는 정리 함수입니다
    정리가 필요한 모든 리소스를 해제하세요
    """
    pass

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    """
    입력 포트에서 신호가 감지될 때마다 호출됩니다.
    
    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 불리언 리스트
        index (int): 방금 변경된 입력 포트의 인덱스. 시스템에 의해 state_update가 트리거된 경우 -1
        state (bool): 수정된 포트의 새 상태 값 (True/False)
        is_changed (bool): 값이 이전 상태에서 실제로 변경되었는지 여부를 나타내는 플래그
    """
    pass