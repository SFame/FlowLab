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

add_reference('System')
from System.IO.Ports import SerialPort, Parity, StopBits
from System.Threading import Thread, ThreadStart
from System import TimeoutException
import time

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
name: str = "Serial Communication Node (IN) - FAST"

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_list: list = ['Connect', 'SetPort']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['Data1', 'Data2', 'Connected', 'DataReceiving']

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. input_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, str
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_types: list = [bool, bool]

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. output_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_types: list = [int, int, bool, bool]

# True일 경우, 이 노드의 메서드를 비동기적으로 실행할 수 있습니다(하지만 terminate()는 언제나 동기적으로 실행됩니다)
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
is_async: bool = True



# <<노드 컨트롤러>>

# ====================== WARNING ======================
# 다음 변수들을 수정하지 마세요
# 이 변수들은 시스템에 의해 자동으로 초기화 됩니다

output_applier: OutputApplier = None
printer: Printer = None
# =====================================================



# <<유틸리티>>

# JSON 유틸리티 객체
json_util: JsonUtil = JsonUtil()

# <<전역 변수>>

# 시리얼 포트 객체
serial_port = None

# 현재 포트 설정
current_port = "COM3"
current_baud_rate = 9600

# 연결 상태
is_connected = False
is_connecting = False

# Arduino에서 받은 데이터 (주요 2개만)
received_data = {
    "data1": 0.0,
    "data2": 0.0
}

# 에러 상태
has_error = False
last_error = ""

# 데이터 수신 상태 (테스트용)
is_data_receiving = False
last_receive_time = 0
receive_timeout = 1.0  # 🚀 2초 → 1초로 단축

# 수신 스레드 중지 플래그
stop_receiving = False

# <<노드 생명주기 메서드>>

def init(inputs: list) -> None:
    """
    노드 초기화
    """
    global is_connected, has_error, received_data, is_data_receiving
    
    # 초기 상태 설정
    is_connected = False
    has_error = False
    is_data_receiving = False
    received_data = {
        "data1": 0,
        "data2": 0
    }
    
    # 초기 출력 설정: [Data1, Data2, Connected, DataReceiving]
    outputs = [0, 0, False, False]
    output_applier.apply(outputs)
    
    printer.print(f"🚀 FAST Serial IN Node Ready - Port: {current_port}")
    printer.print("Arduino → Unity data receiver (OPTIMIZED)")

def terminate() -> None:
    """
    노드 종료 시 정리 작업
    """
    global stop_receiving
    
    # 수신 중지
    stop_receiving = True
    
    # 시리얼 포트 연결 해제
    disconnect_serial()
    
    printer.print("🚀 FAST Serial IN Node terminated")

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    입력 신호 처리 - 즉각 반응 최적화
    """
    global current_port
    
    # 변경이 없으면 무시
    if not is_changed:
        return
    
    # None 값 처리
    if state is None:
        port_names = ['Connect', 'SetPort']
        if index < len(port_names):
            printer.print(f"🚀 {port_names[index]} signal lost")
        return
    
    # 입력별 처리
    if index == 0:  # Connect 신호 (True=연결, False=해제)
        if state:
            connect_serial()
        else:
            disconnect_serial()
        
    elif index == 1 and state:  # SetPort 신호 (포트 변경)
        change_port()

def connect_serial():
    """
    시리얼 포트 연결 - 초고속 최적화
    """
    global serial_port, is_connected, is_connecting, has_error, stop_receiving
    
    if is_connected or is_connecting:
        printer.print("🚀 Already connected or connecting")
        return
    
    try:
        is_connecting = True
        printer.print(f"🚀 Connecting to {current_port}...")
        
        # 시리얼 포트 생성 및 설정 - 🚀 타임아웃 대폭 단축
        serial_port = SerialPort()
        serial_port.PortName = current_port
        serial_port.BaudRate = current_baud_rate
        serial_port.Parity = 0
        serial_port.DataBits = 8
        serial_port.StopBits = StopBits.One
        serial_port.ReadTimeout = 50    # 🚀 1000ms → 50ms
        serial_port.WriteTimeout = 50   # 🚀 1000ms → 50ms
        
        # 포트 열기
        serial_port.Open()
        
        if serial_port.IsOpen:
            is_connected = True
            has_error = False
            printer.print(f"🚀 ✓ Connected to {current_port} (FAST MODE)")
            
            # 🚀 버퍼 클리어 대기시간 최소화
            if serial_port.BytesToRead > 0:
                discarded_bytes = serial_port.BytesToRead
                serial_port.DiscardInBuffer()
                printer.print(f"🚀 🗑️ Cleared {discarded_bytes} bytes")
            
            # 📡 데이터 수신 시작 (별도 스레드에서)
            stop_receiving = False
            start_receiving()
            
            # 연결 상태 출력 업데이트
            update_outputs()
        else:
            raise Exception("Failed to open port")
            
    except Exception as e:
        is_connected = False
        has_error = True
        error_msg = f"Connection failed: {str(e)}"
        printer.print(f"🚀 ✗ {error_msg}")
        
        if serial_port is not None:
            try:
                serial_port.Close()
                serial_port.Dispose()
            except:
                pass
            serial_port = None
        
        update_outputs()
    finally:
        is_connecting = False

def start_receiving():
    """
    데이터 수신 시작 (별도 스레드에서 실행)
    """
    thread_start = ThreadStart(receive_data_loop)
    receive_thread = Thread(thread_start)
    receive_thread.Start()
    printer.print("🚀 Data receiving started (FAST MODE)")

def disconnect_serial():
    """
    시리얼 포트 연결 해제 - 즉시 실행
    """
    global serial_port, is_connected, stop_receiving, has_error
    
    if not is_connected:
        printer.print("🚀 Already disconnected")
        return
    
    try:
        # 수신 중지
        stop_receiving = True
        
        # 포트 닫기
        if serial_port is not None and serial_port.IsOpen:
            serial_port.Close()
            serial_port.Dispose()
            serial_port = None
        
        is_connected = False
        has_error = False
        printer.print("🚀 ✓ Disconnected (INSTANT)")
        
        # 출력 업데이트
        update_outputs()
        
    except Exception as e:
        has_error = True
        printer.print(f"🚀 ✗ Disconnect error: {str(e)}")
        update_outputs()

def receive_data_loop():
    """
    데이터 수신 루프 - 초고속 최적화
    """
    global serial_port, stop_receiving, received_data, has_error, is_data_receiving, last_receive_time
    
    printer.print("🚀 Receive loop started (ULTRA-FAST)")
    
    while not stop_receiving and is_connected:
        try:
            if serial_port is not None and serial_port.IsOpen:
                # 버퍼에 데이터가 있는지 확인
                if serial_port.BytesToRead > 0:
                    # 🚀 최신 데이터만 가져오기: 버퍼에 쌓인 모든 라인 읽기
                    lines = []
                    while serial_port.BytesToRead > 0:
                        try:
                            line = serial_port.ReadLine().strip()
                            if line:
                                lines.append(line)
                        except TimeoutException:
                            break
                    
                    # 가장 최신 데이터만 처리 (마지막 라인)
                    if lines:
                        latest_line = lines[-1]
                        discarded_count = len(lines) - 1
                        
                        if discarded_count > 0:
                            printer.print(f"🚀 ⚡ Discarded {discarded_count} old data, processing latest")
                        
                        # 데이터 수신 시간 업데이트
                        last_receive_time = time.time()
                        is_data_receiving = True
                        
                        # JSON 파싱 시도 (최신 데이터만)
                        success, parsed_data = json_util.try_deserialize(latest_line)
                        
                        if success:
                            # 📡 JSON 데이터 처리 및 실시간 출력 갱신
                            process_received_data(parsed_data)
                        else:
                            printer.print(f"🚀 JSON parse error: {latest_line}")
                else:
                    # 데이터 수신 타임아웃 체크
                    if is_data_receiving and (time.time() - last_receive_time) > receive_timeout:
                        is_data_receiving = False
                        # 📡 타임아웃 시에도 출력 상태 갱신
                        update_outputs()
                        printer.print("🚀 ⚠️ Data receive timeout - no data for 1 second")
                
                # 🚀 대기시간 최소화 (더 빠른 반응)
                time.sleep(0.001)  # 10ms → 1ms로 대폭 단축
                
        except TimeoutException:
            # 타임아웃은 정상적인 상황
            continue
            
        except Exception as e:
            has_error = True
            is_data_receiving = False
            printer.print(f"🚀 Receive error: {str(e)}")
            # 📡 에러 발생 시에도 출력 상태 갱신
            update_outputs()
            break
    
    # 루프 종료 시 데이터 수신 상태 False
    is_data_receiving = False
    update_outputs()
    printer.print("🚀 Receive loop stopped")

def process_received_data(data):
    """
    수신된 JSON 데이터 처리 - 즉시 처리
    """
    global received_data, has_error
    
    try:
        if isinstance(data, dict):
            # 주요 데이터 2개만 추출 (다양한 필드명 지원)
            received_data["data1"] = int(data.get("sensor1", data.get("data1", data.get("value1", 0.0))))
            received_data["data2"] = int(data.get("sensor2", data.get("data2", data.get("value2", 0.0))))
            
            has_error = False
            
            # 🚀 실시간 출력 업데이트 (즉시 반영)
            update_outputs()
            
            # 수신 데이터 로그 (필요시 주석 처리로 더 빠르게)
            printer.print(f"🚀 RX: [{received_data['data1']}, {received_data['data2']}]")
        
    except Exception as e:
        has_error = True
        printer.print(f"🚀 Data processing error: {str(e)}")
        update_outputs()

def update_outputs():
    """
    출력 포트 업데이트 - 즉시 반영
    """
    outputs = [
        received_data["data1"],
        received_data["data2"],
        is_connected,          # 테스트용: 연결 상태
        is_data_receiving      # 테스트용: 데이터 수신 중인지 상태
    ]
    
    output_applier.apply(outputs)

def change_port():
    """
    포트 변경 - 🚀 즉시 변경 (대기시간 제거)
    """
    global current_port
    
    # 연결 중이면 먼저 해제 (대기시간 없이)
    if is_connected:
        disconnect_serial()
        # 🚀 대기시간 제거! (0.5초 → 0초)
    
    # 포트 순환
    if current_port == "COM3":
        current_port = "COM4"
    elif current_port == "COM4":
        current_port = "COM5"
    elif current_port == "COM5":
        current_port = "COM12"
    else:
        current_port = "COM3"
    
    printer.print(f"🚀 🔌 Port changed to: {current_port} (INSTANT)")