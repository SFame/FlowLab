add_reference("System.Threading")
from System.Threading import Thread, ThreadInterruptedException

# Defines the node's name
name: str = "Async Timer"

# Specifies number of input ports
input_list: list = ['Trig', 'Rst', 'Inc', 'Clr']

# Specifies number of output ports
output_list: list = ['Out']

input_types: list = [bool, bool, bool, bool]

output_types: list = [bool]

# When True, allows this Node's methods to be executed asynchronously
is_async: bool = True

# Controls whether state_update is automatically called after initialization
auto_state_update_after_init: bool = True

# Object responsible for applying output signals to the node
output_applier: OutputApplier = None

# Object for displaying values above the node
printer: Printer = None

# 상태 관리 변수
is_timer_running = False
target_time = 1.0  # 기본 1초
time_step = 0.5    # 시간 증가 단위 (0.5초)
update_interval = 0.1  # 타이머 업데이트 간격 (0.1초)

def run_timer():
    global is_timer_running
    
    try:
        # 타이머 시간이 0이면 즉시 완료
        if target_time <= 0:
            printer.print("Timer time is zero!")
            output_applier.apply([True])
            Thread.Sleep(100)
            if is_timer_running:
                output_applier.apply([False])
            is_timer_running = False
            return
            
        # 타이머 시작
        is_timer_running = True
        printer.print(f"Timer started: {target_time:.1f}s")
        
        # 시간 카운트다운
        remaining_time = target_time
        update_count = int(target_time / update_interval)
        
        for i in range(update_count):
            if not is_timer_running:
                return  # 타이머가 중지되었으면 즉시 종료
                
            # 남은 시간 계산 및 표시
            remaining_time = target_time - (i * update_interval)
            printer.print(f"Time left: {remaining_time:.1f}s")
            
            # 일정 시간 대기
            Thread.Sleep(int(update_interval * 1000))
        
        # 마지막 잔여 시간 처리 (나눠떨어지지 않는 경우)
        last_interval = target_time - (update_count * update_interval)
        if last_interval > 0 and is_timer_running:
            Thread.Sleep(int(last_interval * 1000))
        
        # 타이머 완료
        if is_timer_running:
            printer.print("Timer completed!")
            
            # 출력 신호 활성화
            output_applier.apply([True])
            
            # 잠시 후 출력 신호 비활성화
            Thread.Sleep(100)  # 0.1초
            if is_timer_running:
                output_applier.apply([False])
                printer.print("Ready for next trigger")
    except ThreadInterruptedException:
        printer.print("Timer interrupted")
    except Exception as e:
        printer.print(f"Error: {str(e)}")
    finally:
        is_timer_running = False

def init(inputs: list) -> None:
    """초기화 함수"""
    global is_timer_running, target_time
    is_timer_running = False
    target_time = 1.0  # 기본값 1초로 초기화
    
    # 출력 초기화
    output_applier.apply([False])
    printer.print(f"Timer ready: {target_time:.1f}s")

def terminate() -> None:
    """정리 함수"""
    global is_timer_running
    is_timer_running = False  # 실행 중인 타이머 중지

def state_update(inputs: list, index: int, state, is_changed: bool, is_disconnected: bool) -> None:
    """입력 신호 처리 함수"""
    global is_timer_running, target_time
    
    # 입력 변화 없거나 입력이 False로 변했을 때는 무시
    if not is_changed or (index >= 0 and not state):
        return
    
    # 입력 인덱스에 따른 처리
    if index == 0 and state:  # Trigger 신호
        if not is_timer_running:
            # 타이머가 실행 중이 아닐 때만 시작
            run_timer()
            
    elif index == 1 and state:  # Reset 신호
        is_timer_running = False
        output_applier.apply([False])
        printer.print(f"Timer reset: {target_time:.1f}s")
            
    elif index == 2 and state:  # Increase Time 신호
        if not is_timer_running:  # 타이머가 실행 중이 아닐 때만 시간 조정 가능
            target_time += time_step
            printer.print(f"Timer set to: {target_time:.1f}s")
            
    elif index == 3 and state:  # Clear Time 신호
        if not is_timer_running:  # 타이머가 실행 중이 아닐 때만 시간 조정 가능
            target_time = 0.0
            printer.print("Timer cleared to 0.0s")