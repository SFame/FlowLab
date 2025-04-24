# Defines the node's name
name: str = "8-bit CPU Node"
# Specifies number of input ports (클럭, 데이터 입력 8개, 리셋, 명령어 모드 2개)
input_list: list = ['Clock', 'Data 1', 'Data 2', 'Data 3', 'Data 4', 'Data 5', 'Data 6', 'Data 7', 'Data 8', 'Reset', 'Mode 1', 'Mode 2']
# Specifies number of output ports with names
output_list: list = ['Data 1', 'Data 2', 'Data 3', 'Data 4', 'Data 5', 'Data 6', 'Data 7', 'Data 8', 'Zero Flag', 'Carry Flag']
# When True, allows this node's methods to be executed asynchronously
# ※This value is only reflected in the node when initially set; changes after initialization have no effect
is_async: bool = False
# Controls whether state_update is automatically called after initialization
auto_state_update_after_init: bool = True
# Object responsible for applying output signals to the node
output_applier: OutputApplier = None
# Object for displaying values above the node
printer: Printer = None

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

def init() -> None:
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
    return

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    global last_clock, current_op, is_executing, cycle_count
    
    # 입력이 부족하면 작업 중단
    if len(inputs) < 12:
        return
    
    # 리셋 신호 처리 (Reset 입력)
    if index == 9 and state and is_changed:
        init()
        return
    
    # 명령어 모드 변경 감지 (Mode 1 입력)
    if index == 10 and state and is_changed:
        decode_instruction(inputs)
        printer.print(f"Instruction: {get_op_name(current_op)}")
        return
    
    # 클럭 처리 (Clock 입력)
    if index == 0:
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
    
    # 명령어 모드 입력을 해석
    mode1 = inputs[10]  # Mode 1
    mode2 = inputs[11] if len(inputs) > 11 else False  # Mode 2
    
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
    # 입력 1-8을 B 레지스터로 로드
    for i in range(8):
        if i + 1 < len(inputs):
            registers['B'][i] = inputs[i + 1]

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