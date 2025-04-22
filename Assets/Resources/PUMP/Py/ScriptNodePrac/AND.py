# Defines the node's name
name: str = "AND Gate"
# Specifies number of input ports
input_counts: int = 2
# Specifies number of output ports
output_counts: int = 1
# Object responsible for applying output signals to the node
output_applier: OutputApplier = None

def init() -> None:
    # 초기 상태에서 출력 업데이트 수행
    outputs = [False]
    output_applier.apply(outputs)

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    # AND 게이트 로직 구현
    # 두 입력이 모두 True인 경우에만 출력도 True
    result = all(inputs)
    
    # 출력 포트 수에 맞게 출력값 리스트 생성
    outputs = [result]
    
    # 출력 적용
    output_applier.apply(outputs)