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
# add_reference('필요_어셈블리')
# from 필요_어셈블리.필요_네임스페이스 import 필요_클래스

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



# Scripting Node는 아래의 멤버를 반드시 포함해야 합니다
# ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓

# <<노드 속성>>

# 노드의 이름 정의
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
name: str = "8-bit CPU Node"

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_list: list = ['Clk', 'D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7', 'D8', 'Rst', 'M1', 'M2']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7', 'D8', 'Zero', 'Carry']

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. input_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, str
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_types: list = [bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool]

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. output_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, str
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_types: list = [bool, bool, bool, bool, bool, bool, bool, bool, bool, bool]

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
output_applier: OutputApplier = None

# 프린터 객체
# <사용 가능한 API> 
#   printer.print(value: str) -> None:
# 노드의 디스플레이에 문자열 정보를 표시하는 데 사용됩니다
printer: Printer = None
# =====================================================

# CPU 상태 및 레지스터
registers = {
    'A': [False] * 8,  # 누산기 (Accumulator)
    'B': [False] * 8,  # 보조 레지스터
    'PC': [False] * 8  # 프로그램 카운터
}

# 플래그
flags = {
    'Z': False,  # 제로 플래그
    'C': False   # 캐리 플래그
}

# 명령어 집합
OP_NOP = 0   # 아무 작업 안함
OP_LOAD = 1  # B 레지스터 값을 A로 로드
OP_ADD = 2   # A + B (결과는 A에 저장)
OP_SUB = 3   # A - B (결과는 A에 저장)
OP_AND = 4   # A AND B (결과는 A에 저장)
OP_OR = 5    # A OR B (결과는 A에 저장)
OP_XOR = 6   # A XOR B (결과는 A에 저장)
OP_NOT = 7   # NOT A (결과는 A에 저장)
OP_SHL = 8   # A 왼쪽 시프트 (결과는 A에 저장)
OP_SHR = 9   # A 오른쪽 시프트 (결과는 A에 저장)

# 현재 명령어
current_op = OP_NOP

# 상태 추적
last_clock = False  # 마지막 클럭 상태
is_executing = False  # 실행 중인지 여부
cycle_count = 0  # 사이클 카운터



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
    # 초기화 - 모든 레지스터와 플래그 리셋
    for reg in registers:
        registers[reg] = [False] * 8
    
    flags['Z'] = False
    flags['C'] = False
    
    global current_op, last_clock, is_executing, cycle_count
    current_op = OP_NOP
    last_clock = False
    is_executing = False
    cycle_count = 0
    
    # 출력 초기화 (8비트 데이터 출력 + 2개 플래그)
    outputs = [False] * 10
    output_applier.apply(outputs)
    printer.print("CPU Reset - Ready")

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
    global last_clock, current_op, is_executing, cycle_count
    
    # None 값 처리 - 연결이 해제되거나 신호가 소실된 경우
    if state is None:
        # 중요한 신호들이 소실된 경우 적절히 처리
        if index == 0:  # 클럭 신호 소실
            printer.print("Clock signal lost")
            return
        elif index == 9:  # 리셋 신호 소실
            printer.print("Reset signal lost")
            return
    
    # 리셋 신호 처리 (Reset 입력)
    if index == 9 and state and is_changed:
        init(inputs)
        return
    
    # 명령어 모드 변경 감지 (Mode 1 입력)
    if index == 10 and state and is_changed:
        decode_instruction(inputs)
        printer.print(f"Instruction: {get_op_name(current_op)}")
        return
    
    # 클럭 처리 (Clock 입력)
    if index == 0 and state is not None:
        # 클럭 상승 에지 감지
        if state and not last_clock:
            execute_cycle(inputs)
        last_clock = state
    
    # 명령어가 없거나 클럭이 아닌 경우에도 입력 데이터를 처리
    if index > 0 and index < 9 and is_changed:
        # 데이터 입력 (입력 1-8)이 변경되면 B 레지스터 업데이트
        update_register_from_inputs(inputs)
    
    # 출력 상태 업데이트
    update_outputs()

def decode_instruction(inputs):
    global current_op
    
    # None 값 체크 후 명령어 모드 입력을 해석
    mode1 = inputs[10] if inputs[10] is not None else False  # Mode 1
    mode2 = inputs[11] if inputs[11] is not None else False  # Mode 2
    
    if not mode1 and not mode2:
        current_op = OP_LOAD if cycle_count % 2 == 0 else OP_ADD
    elif mode1 and not mode2:
        current_op = OP_SUB if cycle_count % 2 == 0 else OP_AND
    elif not mode1 and mode2:
        current_op = OP_OR if cycle_count % 2 == 0 else OP_XOR
    else:
        current_op = OP_NOT if cycle_count % 2 == 0 else OP_SHL

def execute_cycle(inputs):
    global cycle_count, is_executing
    
    # 사이클 증가
    cycle_count += 1
    
    # 실행 시작
    is_executing = True
    
    # 명령어 실행
    execute_instruction(inputs)
    
    # 상태 플래그 업데이트
    update_flags()
    
    # 출력 업데이트
    update_outputs()
    
    # 실행 종료
    is_executing = False
    
    # 사이클 정보 출력
    printer.print(f"Cycle: {cycle_count}, OP: {get_op_name(current_op)}")

def execute_instruction(inputs):
    # 현재 명령어 실행
    if current_op == OP_NOP:
        # 아무 작업 없음
        pass
    
    elif current_op == OP_LOAD:
        # B 레지스터 값을 A로 로드
        registers['A'] = registers['B'].copy()
    
    elif current_op == OP_ADD:
        # A + B
        result, carry = add_8bit(registers['A'], registers['B'])
        registers['A'] = result
        flags['C'] = carry
    
    elif current_op == OP_SUB:
        # A - B
        result, borrow = sub_8bit(registers['A'], registers['B'])
        registers['A'] = result
        flags['C'] = borrow
    
    elif current_op == OP_AND:
        # A AND B
        for i in range(8):
            registers['A'][i] = registers['A'][i] and registers['B'][i]
    
    elif current_op == OP_OR:
        # A OR B
        for i in range(8):
            registers['A'][i] = registers['A'][i] or registers['B'][i]
    
    elif current_op == OP_XOR:
        # A XOR B
        for i in range(8):
            registers['A'][i] = (registers['A'][i] or registers['B'][i]) and not (registers['A'][i] and registers['B'][i])
    
    elif current_op == OP_NOT:
        # NOT A
        for i in range(8):
            registers['A'][i] = not registers['A'][i]
    
    elif current_op == OP_SHL:
        # 왼쪽 시프트
        carry = registers['A'][0]
        for i in range(7):
            registers['A'][i] = registers['A'][i+1]
        registers['A'][7] = False
        flags['C'] = carry
    
    elif current_op == OP_SHR:
        # 오른쪽 시프트
        carry = registers['A'][7]
        for i in range(7, 0, -1):
            registers['A'][i] = registers['A'][i-1]
        registers['A'][0] = False
        flags['C'] = carry

def update_register_from_inputs(inputs):
    # 입력 1-8을 B 레지스터로 로드 (None 값 처리 포함)
    for i in range(8):
        registers['B'][i] = inputs[i + 1] if inputs[i + 1] is not None else False

def update_outputs():
    # A 레지스터 값과 플래그를 출력으로 설정
    outputs = registers['A'].copy()
    outputs.append(flags['Z'])
    outputs.append(flags['C'])
    output_applier.apply(outputs)

def update_flags():
    # 제로 플래그 업데이트
    flags['Z'] = all(not bit for bit in registers['A'])

def add_8bit(a, b):
    result = [False] * 8
    carry = False
    
    for i in range(7, -1, -1):
        # 1비트 덧셈 수행
        sum_bit = (a[i] != b[i]) != carry
        next_carry = (a[i] and b[i]) or (a[i] and carry) or (b[i] and carry)
        
        result[i] = sum_bit
        carry = next_carry
    
    return result, carry

def sub_8bit(a, b):
    result = [False] * 8
    borrow = False
    
    for i in range(7, -1, -1):
        # 1비트 뺄셈 수행
        diff_bit = (a[i] != b[i]) != borrow
        next_borrow = (not a[i] and b[i]) or (not a[i] and borrow) or (b[i] and borrow)
        
        result[i] = diff_bit
        borrow = next_borrow
    
    return result, borrow

def get_op_name(op):
    op_names = {
        OP_NOP: "NOP",
        OP_LOAD: "LOAD B→A",
        OP_ADD: "ADD A+B",
        OP_SUB: "SUB A-B",
        OP_AND: "AND A&B",
        OP_OR: "OR A|B",
        OP_XOR: "XOR A^B",
        OP_NOT: "NOT ~A",
        OP_SHL: "SHIFT LEFT",
        OP_SHR: "SHIFT RIGHT"
    }
    return op_names.get(op, "UNKNOWN")