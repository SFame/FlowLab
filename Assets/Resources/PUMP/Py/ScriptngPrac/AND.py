# Defines the node's name
name: str = "AND Gate"
# Specifies number of input ports
input_list: list = ['A', 'B']
# Specifies number of output ports
output_list: list = ['Y']
input_types: list = [bool, bool]
output_types: list = [bool]
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

def init(inputs: list) -> None:
    # 초기 상태에서 출력 업데이트 수행
    result = all(inputs)
    outputs = [result]
    output_applier.apply(outputs)

def terminate() -> None:
    return

def state_update(inputs: list, index: int, state, is_changed: bool, is_disconnected: bool) -> None:
    # AND 게이트 로직 구현
    # 두 입력이 모두 True인 경우에만 출력도 True
    result = all(inputs)
    
    # 출력 포트 수에 맞게 출력값 리스트 생성
    outputs = [result]
    
    # 출력 적용
    output_applier.apply(outputs)