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
name: str = "Serial Communication Node (Bidirectional)"

# 입력 포트: Connect, SetPort + 다른 노드에서 받을 데이터 2개
input_list: list = ['Connect', 'SetPort', 'Motor1', 'Motor2']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['Sensor1', 'Sensor2', 'Connected', 'DataReceiving']

# 아래의 리스트를 설정하여 포트의 타입을 설정합니다. input_list의 길이와 일치해야 합니다
# 사용 가능한 타입: bool, int, float, str
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_types: list = [bool, bool, int, int]

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



# <<유틸리티>>

# JSON 유틸리티 객체
# <사용 가능한 API>
#   json_util.serialize(data, pretty: bool=False) -> str:
#   json_util.deserialize(json_text: str) -> object:
#   json_util.try_serialize(data, pretty: bool=False) -> (bool, str):
#   json_util.try_deserialize(json_text: str) -> (bool, object):
#   json_util.is_valid(json_text: str) -> bool:
json_util: JsonUtil = JsonUtil()

# <<전역 변수>>

# 시리얼 포트 객체
serial_port = None

# 현재 포트 설정
current_port = "COM12"
current_baud_rate = 9600

# 연결 상태
is_connected = False
is_connecting = False

# 📡 Arduino에서 받은 센서 데이터 (수신)
sensor_data = {
    "sensor1": 0,
    "sensor2": 0
}

# 📤 Arduino로 보낼 모터 제어 데이터 (송신)
motor_data = {
    "motor1": 0,
    "motor2": 0
}

# 에러 상태
has_error = False
last_error = ""

# 데이터 수신 상태 (테스트용)
is_data_receiving = False
last_receive_time = 0
receive_timeout = 1.0  # 2초간 데이터 없으면 False

# 수신/송신 스레드 중지 플래그
stop_receiving = False
stop_sending = False
# 전송 설정
send_interval = 0.1  # 100ms 간격으로 전송
send_count = 0

# <<노드 생명주기 메서드>>

def init(inputs: list) -> None:
    """
    노드 초기화
    """
    global is_connected, has_error, sensor_data, motor_data, is_data_receiving, stop_receiving, stop_sending
    
    try:
        # 초기 상태 설정
        is_connected = False
        has_error = False
        is_data_receiving = False
        stop_receiving = False
        stop_sending = True  # 초기에는 전송 중지
        
        # 센서 데이터 초기화
        sensor_data = {
            "sensor1": 0,
            "sensor2": 0
        }
        
        # 모터 데이터 초기화 (입력값 반영)
        motor_data = {
            "motor1": 0,
            "motor2": 0
        }
        
        if inputs is not None and len(inputs) >= 4:
            try:
                motor_data["motor1"] = int(inputs[2]) if inputs[2] is not None else 0
                motor_data["motor2"] = int(inputs[3]) if inputs[3] is not None else 0
            except (ValueError, TypeError, IndexError):
                motor_data["motor1"] = 0
                motor_data["motor2"] = 0
                if printer is not None:
                    printer.print("🔄 ⚠️ Invalid input data types, using defaults")
        
        # 초기 출력 설정: [Sensor1, Sensor2, Connected, DataReceiving]
        outputs = [0, 0, False, False]
        output_applier.apply(outputs)
        
        if printer is not None:
            printer.print(f"🔄 Bidirectional Serial Node Ready - Port: {current_port}")
            printer.print("🔄 Arduino ↔ Unity real-time communication")
            printer.print(f"🔄 Initial motor values: [{motor_data['motor1']}, {motor_data['motor2']}]")
        
    except Exception as e:
        if printer is not None:
            printer.print(f"🔄 ✗ Init error: {str(e)}")
        # 에러 발생 시에도 기본값으로 초기화
        sensor_data = {"sensor1": 0, "sensor2": 0}
        motor_data = {"motor1": 0, "motor2": 0}
        is_connected = False
        stop_receiving = False
        stop_sending = True


def terminate() -> None:
  
    """
    노드 종료 시 정리 작업
    """
    global stop_receiving, stop_sending
    
    # 수신/송신 중지
    stop_receiving = True
    stop_sending = True
    
    # 시리얼 포트 연결 해제
    disconnect_serial()
    
    if printer is not None:
        printer.print("🔄 Bidirectional Serial Node terminated")
        
def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    입력 신호 처리
    """
    global current_port, motor_data
    
    # 변경이 없으면 무시
    if not is_changed:
        return
    
    # None 값 처리
    if state is None:
        port_names = ['Connect', 'SetPort', 'Motor1', 'Motor2']
        if index < len(port_names) and printer is not None:
            printer.print(f"🔄 {port_names[index]} signal lost")
        return
    
    # 입력별 처리
    if index == 0:  # Connect 신호 (True=연결, False=해제)
        if state:
            connect_serial()
        else:
            disconnect_serial()
        
    elif index == 1 and state:  # SetPort 신호 (포트 변경)
        change_port()
        
    elif index == 2:  # Motor1 입력 - 데이터만 업데이트 (전송은 별도)
        old_value = motor_data["motor1"]
        motor_data["motor1"] = int(state) if state is not None else 0
        if printer is not None:
            printer.print(f"🔄 Motor1 updated: {old_value} → {motor_data['motor1']}")
        
    elif index == 3:  # Motor2 입력 - 데이터만 업데이트 (전송은 별도)
        old_value = motor_data["motor2"]
        motor_data["motor2"] = int(state) if state is not None else 0
        if printer is not None:
            printer.print(f"🔄 Motor2 updated: {old_value} → {motor_data['motor2']}")

def connect_serial():
    """
    시리얼 포트 연결 (양방향 통신 시작)
    """
    global serial_port, is_connected, is_connecting, has_error, stop_receiving, stop_sending
    
    if is_connected or is_connecting:
        if printer is not None:
            printer.print("🔄 Already connected or connecting")
        return
    
    try:
        is_connecting = True
        if printer is not None:
            printer.print(f"🔄 Connecting to {current_port}...")
        
        # 시리얼 포트 생성 및 설정
        serial_port = SerialPort()
        serial_port.PortName = current_port
        serial_port.BaudRate = current_baud_rate
        serial_port.Parity = 0  # None (패리티 없음)
        serial_port.DataBits = 8
        serial_port.StopBits = StopBits.One
        serial_port.ReadTimeout = 100   # 매우 짧은 타임아웃
        serial_port.WriteTimeout = 100
        
        # 포트 열기
        serial_port.Open()
        
        if serial_port.IsOpen:
            is_connected = True
            has_error = False
            if printer is not None:
                printer.print(f"🔄 ✓ Connected to {current_port} (STREAMING MODE)")
            
            # 🔥 연결 직후 버퍼 클리어 (이전에 쌓인 오래된 데이터 제거)
            time.sleep(0.1)  # 잠시 대기
            if serial_port.BytesToRead > 0:
                discarded_bytes = serial_port.BytesToRead
                serial_port.DiscardInBuffer()
                if printer is not None:
                    printer.print(f"🔄 🗑️ Cleared {discarded_bytes} bytes from buffer")
            
            # 📡 데이터 수신 시작 (별도 스레드에서)
            stop_receiving = False
            start_receiving()
            
            # 📤 데이터 전송 시작 (별도 스레드에서)
            stop_sending = False
            start_sending()
            
            # 연결 상태 출력 업데이트
            update_outputs()
        else:
            raise Exception("Failed to open port")
            
    except Exception as e:
        is_connected = False
        has_error = True
        error_msg = f"Connection failed: {str(e)}"
        if printer is not None:
            printer.print(f"🔄 ✗ {error_msg}")
        
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

def disconnect_serial():
    """
    시리얼 포트 연결 해제
    """
    global serial_port, is_connected, stop_receiving, stop_sending, has_error, is_data_receiving
    
    if not is_connected:
        if printer is not None:
            printer.print("🔄 Already disconnected")
        return
    
    try:
        # 수신/송신 중지
        stop_receiving = True
        stop_sending = True
        
        # 포트 닫기
        if serial_port is not None and serial_port.IsOpen:
            serial_port.Close()
            serial_port.Dispose()
            serial_port = None
        
        is_connected = False
        has_error = False
        is_data_receiving = False
        if printer is not None:
            printer.print("🔄 ✓ Disconnected (INSTANT)")
        
        # 출력 업데이트
        update_outputs()
        
    except Exception as e:
        has_error = True
        if printer is not None:
            printer.print(f"🔄 ✗ Disconnect error: {str(e)}")
        update_outputs()

def start_receiving():
    """
    데이터 수신 시작 (별도 스레드에서 실행)
    """
    thread_start = ThreadStart(receive_data_loop)
    receive_thread = Thread(thread_start)
    receive_thread.Start()
    if printer is not None:
        printer.print("🔄 📡 Streaming thread started")

def start_sending():
    """
    데이터 전송 시작 (별도 스레드에서 실행)
    """
    thread_start = ThreadStart(send_data_loop)
    send_thread = Thread(thread_start)
    send_thread.Start()
    if printer is not None:
        printer.print(f"🔄 📤 Continuous sending started (every {send_interval*1000:.0f}ms)")

def receive_data_loop():
    """
    데이터 수신 루프 - 연속 스트리밍 모드 (Arduino → Unity)
    """
    global serial_port, stop_receiving, sensor_data, has_error, is_data_receiving, last_receive_time
    
    if printer is not None:
        printer.print("🔄 📡 Continuous streaming started")
    
    while not stop_receiving and is_connected:
        try:
            if serial_port is not None and serial_port.IsOpen:
                # 🚀 연속으로 데이터 읽기 시도
                try:
                    line = serial_port.ReadLine().strip()
                    
                    if line:
                        # 데이터 수신 시간 업데이트
                        last_receive_time = time.time()
                        is_data_receiving = True
                        
                        # JSON 파싱 시도
                        success, parsed_data = json_util.try_deserialize(line)
                        
                        if success:
                            # 📡 JSON 데이터 처리 및 실시간 출력 갱신
                            process_received_data(parsed_data)
                        else:
                            if printer is not None:
                                printer.print(f"🔄 JSON parse error: {line}")
                            
                except TimeoutException:
                    # 타임아웃 발생 시 데이터 수신 상태 체크
                    if is_data_receiving and (time.time() - last_receive_time) > receive_timeout:
                        is_data_receiving = False
                        update_outputs()
                        if printer is not None:
                            printer.print("🔄 ⚠️ Data stream timeout")
                    continue
                    
        except Exception as e:
            has_error = True
            is_data_receiving = False
            if printer is not None:
                printer.print(f"🔄 Stream error: {str(e)}")
            update_outputs()
            break
    
    # 루프 종료 시 데이터 수신 상태 False
    is_data_receiving = False
    update_outputs()
    if printer is not None:
        printer.print("🔄 Streaming stopped")

def send_data_loop():
    """
    주기적 전송 루프 (별도 스레드에서 실행) (Unity → Arduino)
    """
    global stop_sending, send_count
    
    if printer is not None:
        printer.print("🔄 📤 Send loop started")
    
    while not stop_sending and is_connected:
        try:
            if serial_port is not None and serial_port.IsOpen:
                # 주기적으로 현재 데이터 전송
                send_data_to_arduino()
                
                # 전송 간격 대기
                time.sleep(send_interval)
            else:
                # 포트가 닫혔으면 루프 종료
                break
                
        except Exception as e:
            if printer is not None:
                printer.print(f"🔄 📤 Send loop error: {str(e)}")
            break
    
    if printer is not None:
        printer.print("🔄 📤 Send loop stopped")

def process_received_data(data):
    """
    수신된 JSON 데이터 처리 (Arduino → Unity)
    """
    global sensor_data, has_error
    
    try:
        if isinstance(data, dict):
            # 주요 데이터 2개만 추출 (다양한 필드명 지원)
            sensor_data["sensor1"] = int(data.get("sensor1", data.get("data1", data.get("value1", 0))))
            sensor_data["sensor2"] = int(data.get("sensor2", data.get("data2", data.get("value2", 0))))
            
            has_error = False
            
            # 🔥 실시간 출력 업데이트 (즉시 반영)
            update_outputs()
            
            # 수신 데이터 로그
            if printer is not None:
                printer.print(f"🔄 📡 RX: [{sensor_data['sensor1']}, {sensor_data['sensor2']}]")
        
    except Exception as e:
        has_error = True
        if printer is not None:
            printer.print(f"🔄 Data processing error: {str(e)}")
        update_outputs()

def send_data_to_arduino():
    """
    현재 데이터를 Arduino로 전송 (주기적 호출)
    """
    global serial_port, motor_data, send_count
    
    if not is_connected or serial_port is None:
        # 연결되지 않은 상태에서는 조용히 무시
        return
    
    try:
        # JSON 형태로 데이터 생성
        json_data = {
            "speedL": motor_data["motor1"],
            "speedR": motor_data["motor2"],
            "seq": send_count,
            "time": int(time.time() * 1000)
        }
        
        # JSON 문자열로 변환 및 전송
        json_string = json_util.serialize(json_data)
        message = json_string + '\n'
        
        serial_port.Write(message)
        
        # 전송 통계 업데이트
        send_count += 1
        
        # 전송 로그 (50회마다 출력으로 스팸 방지)
        if send_count % 50 == 0 and printer is not None:
            printer.print(f"🔄 📤 TX #{send_count}: [{motor_data['motor1']}, {motor_data['motor2']}] (every {send_interval*1000:.0f}ms)")

        
    except Exception as e:
        if printer is not None:
            printer.print(f"🔄 Send error: {str(e)}")
        # 전송 실패 시 연결 해제
        disconnect_serial()

def update_outputs():
    """
    출력 포트 업데이트
    """
    outputs = [
        sensor_data["sensor1"],
        sensor_data["sensor2"],
        is_connected,          # 테스트용: 연결 상태
        is_data_receiving      # 테스트용: 데이터 수신 중인지 상태
    ]
    
    output_applier.apply(outputs)
    
def change_port():
    """
    포트 변경 (COM12 → COM3 → COM4 → COM5 → COM12 순환)
    """
    global current_port
    
    # 연결 중이면 먼저 해제
    if is_connected:
        disconnect_serial()
        time.sleep(0.5)  # 잠시 대기
    
    # 포트 순환
    if current_port == "COM12":
        current_port = "COM3"
    elif current_port == "COM3":
        current_port = "COM4"
    elif current_port == "COM4":
        current_port = "COM5"
    else:
        current_port = "COM12"
    
    printer.print(f"📤 🔌 Port changed to: {current_port}")