# Defines the node's name
name: str = "ALU Node"
# Specifies number of input ports
input_list: list = ['in 1', 'in 2', 'Mode']
# Specifies number of output ports
output_list: list = ['out 1', 'out 2']
input_types: list = [bool, bool, bool]
output_types: list = [bool, bool]
# When True, allows this node's methods to be executed asynchronously
# ※This value is only reflected in the node when initially set; changes after initialization have no effect
is_async: bool = False
# Controls whether state_update is automatically called after initialization
# When True, system will call state_update once after init() with
# index=-1, state=False, is_changed=False
auto_state_update_after_init: bool = False
# Object responsible for applying output signals to the node
output_applier: OutputApplier = None

# Object responsible for displaying values above the node
# Available API: def print(self, value: str) -> None:
# Used to show string information on the Node.
printer: Printer = None

# Operation modes (defined as constants)
MODE_AND = 0
MODE_OR = 1
MODE_XOR = 2
MODE_ADD = 3
MODE_SUB = 4
MODE_MUL = 5
MODE_DIV = 6

# Mode names for display
MODE_NAMES = ["AND", "OR", "XOR", "ADD", "SUB", "MUL", "DIV"]

# Current operation mode
current_mode = MODE_AND

def init(inputs: list) -> None:
    # 초기 상태에서 출력 업데이트 수행
    outputs = [False, False]
    output_applier.apply(outputs)
    # 초기 모드 표시
    printer.print(f"Mode: {MODE_NAMES[current_mode]}")

def terminate() -> None:
    return

def state_update(inputs: list, index: int, state, is_changed: bool, is_disconnected: bool) -> None:
    global current_mode
    
    # 컨트롤 입력이 True로 변경되면 모드 변경
    if index == 2 and state and is_changed:
        current_mode = (current_mode + 1) % 7  # 7가지 모드를 순환
        # 모드 변경 시 현재 모드를 printer로 표시
        printer.print(f"Mode: {MODE_NAMES[current_mode]}")
    
    # 입력이 충분하지 않으면 연산 불가
    if len(inputs) < 2:
        output_applier.apply([False, False])
        return
    
    # 연산 수행
    result1 = False  # 기본 결과
    result2 = False  # 두 번째 출력 (오버플로우, 캐리, 에러 등)
    
    a = inputs[0]
    b = inputs[1]
    
    # 모드에 따른 연산 수행
    if current_mode == MODE_AND:
        result1 = a and b
    elif current_mode == MODE_OR:
        result1 = a or b
    elif current_mode == MODE_XOR:
        result1 = (a or b) and not (a and b)  # XOR 구현
    elif current_mode == MODE_ADD:
        # 이진 덧셈 (a + b)
        result1 = (a and not b) or (not a and b)  # 합
        result2 = a and b  # 캐리
    elif current_mode == MODE_SUB:
        # 이진 뺄셈 (a - b)
        result1 = (a and not b) or (not a and b)  # 차
        result2 = not a and b  # 빌림
    elif current_mode == MODE_MUL:
        # 곱셈은 AND와 동일 (이진에서)
        result1 = a and b
    elif current_mode == MODE_DIV:
        # 나눗셈은 이진에서 복잡하므로 간단히 구현
        # b가 False(0)이면 에러
        if b:
            result1 = a  # a를 그대로 전달
        else:
            result1 = False
            result2 = True  # 에러 표시
    
    # 출력 적용
    outputs = [result1, result2]
    output_applier.apply(outputs)