# ===============================================================
# Delayed Apply Test Node
# is_async = True 상태에서:
#  1) 신호 수신 → 2초 대기 → apply
#  2) 동시 신호 유입 시 워커 스레드 병렬 동작 관찰
#  3) 대기 중 노드 삭제 시 늦은 apply 처리(terminate 경합) 관찰
# ===============================================================

import time
import threading

# <<Node Configuration>>

name: str = 'Delayed Apply Test'

input_list: list = ['Trigger']
output_list: list = ['Fired', 'Count']

input_types: list = [Pulse]
output_types: list = [Pulse, int]

is_async: bool = True


# <<Node Controllers>>
output_applier: OutputApplier = None
printer: Printer = None


# <<Global Variables>>
count = 0
count_lock = threading.Lock()


# <<Node Lifecycle Methods>>

def init(inputs: list) -> None:
    global count
    count = 0
    output_applier.apply([None, 0])
    printer.print('Ready (async)')


def terminate() -> None:
    # 워커가 sleep 중일 때 노드를 삭제하면
    # 이 로그 "이후"에 #N applying 이 시도되는지 관찰
    printer.print('terminate called')


def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    global count

    # 동시 진입 대비: 카운터만 락으로 보호
    with count_lock:
        count += 1
        my_id = count

    thread_name = threading.current_thread().name
    printer.print(f'#{my_id} received (thread: {thread_name}), sleeping 2s...')

    time.sleep(2.0)

    printer.print(f'#{my_id} applying')
    output_applier.apply([Pulse(), my_id])