name: str = "Simple Controller"
input_list: list = ['Stop Signal']
output_list: list = ['Stopped']
input_types: list = [Pulse]
output_types: list = [Pulse]
is_async: bool = True

# <<노드 컨트롤러>>
output_applier: OutputApplier = None
printer: Printer = None

# <<유틸리티>>
json_util: JsonUtil = JsonUtil()

def init(inputs: list) -> None:
    """노드 초기화"""
    printer.print("노드 초기화됨")

def terminate() -> None:
    """노드 정리"""
    pass

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    printer.print(f"{state}, {is_changed}")
    