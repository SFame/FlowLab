add_reference("System")

from System.Net import WebClient
from System.Threading import Thread, ThreadStart

# 노드의 이름 정의
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
name: str = "HTTP Request"

# 아래의 리스트를 설정하여 입력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
input_list: list = ['GET', 'POST', 'Set URL', 'Set Data']

# 아래의 리스트를 설정하여 출력 포트의 수와 이름을 설정합니다
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
output_list: list = ['Success', 'Error']

input_types: list = [bool, bool, bool, bool]
output_types: list = [bool, bool]

# True일 경우, 이 노드의 메서드를 비동기적으로 실행할 수 있습니다(하지만 terminate()는 언제나 동기적으로 실행됩니다)
# ※이 값은 초기 설정 시에만 노드에 반영됩니다. 함수 내부에서의 변경은 효과가 없습니다
is_async: bool = True

# ====================== WARNING ======================
# 다음 변수들을 수정하지 마세요
# 이 변수들은 시스템에 의해 자동으로 초기화 됩니다

# 출력 포트를 제어하는 객체
# 사용 가능한 API: def apply(self, outputs: list) -> None:
# 입력으로 불리언 리스트를 제공해야 합니다. 이 리스트의 길이는 출력 포트 수와 일치해야 합니다
output_applier: OutputApplier = None

# 프린터 객체
# 사용 가능한 API: def print(self, value: str) -> None:
# 노드의 디스플레이에 문자열 정보를 표시하는 데 사용됩니다
printer: Printer = None
# =====================================================

# 전역 변수
url = "https://httpbin.org/get"
data = "{\"key\": \"value\"}"
response_content = ""
last_error = ""
is_requesting = False
current_method = ""  # 현재 요청 메서드를 저장할 변수

def init(inputs: list) -> None:
    """
    노드가 새로 생성되거나 실행취소/다시실행 작업 중에 호출되는 초기화 함수입니다
    최초 출력 포트들의 상태를 설정하는 용도로 적합합니다
    간결하고 효율적으로 작성하세요

    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 불리언 리스트. 요소의 값을 변경하지 말고 읽기만 하십시오
    """
    global url, data, response_content, last_error, is_requesting
    
    url = "https://httpbin.org/get"
    data = "{\"key\": \"value\"}"
    response_content = ""
    last_error = ""
    is_requesting = False
    
    # 출력 초기화
    output_applier.apply([False, False])
    printer.print(f"URL: {url}")

def terminate() -> None:
    """
    노드가 삭제되거나 실행취소/다시실행 작업 중에 호출되는 정리 함수입니다
    정리가 필요한 모든 리소스를 해제하세요
    """
    global is_requesting
    is_requesting = False  # 진행 중인 요청 중지 플래그

def thread_function():
    """스레드에서 실행될 함수"""
    global current_method
    make_request(current_method)

def make_request(method):
    """HTTP 요청 함수"""
    global response_content, last_error, is_requesting, url, data
    
    is_requesting = True
    try:
        client = WebClient()
        
        if method == "GET":
            # GET 요청
            response_content = client.DownloadString(url)
            
            # 출력 신호 설정
            if is_requesting:  # 중간에 중단되지 않았으면
                output_applier.apply([True, False])
                printer.print(f"GET response: {len(response_content)} bytes")
                printer.print(response_content[:100] + "..." if len(response_content) > 100 else response_content)
                
        elif method == "POST":
            # POST 요청
            client.Headers.Add("Content-Type", "application/json")
            response_content = client.UploadString(url, data)
            
            # 출력 신호 설정
            if is_requesting:  # 중간에 중단되지 않았으면
                output_applier.apply([True, False])
                printer.print(f"POST response: {len(response_content)} bytes")
                printer.print(response_content[:100] + "..." if len(response_content) > 100 else response_content)
                
        return True
        
    except Exception as e:
        last_error = str(e)
        if is_requesting:  # 중간에 중단되지 않았으면
            output_applier.apply([False, True])
            printer.print(f"Error: {last_error}")
        return False
    finally:
        is_requesting = False

def run_request(method):
    """쓰레드를 생성하고 HTTP 요청 실행"""
    global current_method
    
    # 현재 요청 메서드 저장
    current_method = method
    
    try:
        # 명시적으로 ThreadStart 델리게이트 생성
        thread_start = ThreadStart(thread_function)
        thread = Thread(thread_start)
        thread.Start()
    except Exception as e:
        # 오류 발생 시 동기 실행으로 폴백
        printer.print(f"Thread error: {str(e)}, running synchronously")
        make_request(method)

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    입력 포트에서 신호가 감지될 때마다 호출됩니다.
    모든 매개변수를 사용할 필요는 없습니다. 대부분의 경우 inputs만 사용하면 됩니다
    하지만 "신호가 들어온 포트"의 종류, 해당 포트의 상태, 이전 상태와의 차이를 알고 싶다면 나머지 매개변수도 사용하세요
    
    매개변수:
        inputs (list): 각 입력 포트의 상태를 나타내는 불리언 리스트
        index (int): 방금 변경된 입력 포트의 인덱스. 시스템에 의해 state_update가 트리거된 경우 -1
        state (bool): 수정된 포트의 새 상태 값 (True/False)
        is_changed (bool): 변경된 포트의 상태가 이전과 다른지 여부를 나타내는 플래그
    """
    global url, data
    
    # 입력 변화 없거나 입력이 False로 변했을 때는 무시
    if not is_changed or (index >= 0 and not state):
        return
        
    # 입력 인덱스에 따른 처리
    if index == 0 and state:  # GET 신호
        printer.print(f"GET request to {url}")
        run_request("GET")
            
    elif index == 1 and state:  # POST 신호
        printer.print(f"POST request to {url}")
        run_request("POST")
            
    elif index == 2 and state:  # Set URL 신호
        # 예시로 URL 변경 - 실제로는 사용자 입력을 받아야 함
        url = "https://httpbin.org/post"
        printer.print(f"URL set to: {url}")
            
    elif index == 3 and state:  # Set Data 신호
        # 예시로 데이터 변경 - 실제로는 사용자 입력을 받아야 함
        data = "{\"name\": \"test\", \"value\": 123}"
        printer.print(f"Data set to: {data}")