# ===============================================================
# HTTP POST 테스트 (.NET WebClient)
# ===============================================================

add_reference('System')
from System.Net import WebClient
from System.Text import Encoding
import json

name: str = 'HTTP POST (.NET)'

input_list: list = ['▶ Send', 'URL', 'JSON Body', 'Timeout']
output_list: list = ['▶ Success', '▶ Error', 'Response', 'Status Code', 'Error Msg']

input_types: list = [Pulse, str, str, float]
output_types: list = [Pulse, Pulse, str, int, str]

is_async: bool = True

output_applier: OutputApplier = None
printer: Printer = None
json_util: JsonUtil = JsonUtil()

def init(inputs: list) -> None:
    output_applier.apply([None, None, "Ready", 0, ""])
    printer.print("HTTP POST Test initialized")

def terminate() -> None:
    printer.print("HTTP POST Test terminated")

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    
    # Send 신호
    if index == 0 and state is not None:
        url = inputs[1]
        json_body = inputs[2]
        
        # URL 검증
        if not url or url == "":
            output_applier.apply([None, Pulse(), "No URL", 0, "URL is required"])
            printer.print("ERROR: No URL provided")
            return
        
        # JSON Body 검증
        if not json_body or json_body == "":
            output_applier.apply([None, Pulse(), "No Body", 0, "JSON body is required"])
            printer.print("ERROR: No JSON body provided")
            return
        
        try:
            printer.print(f"Sending POST to: {url}")
            
            # JSON 검증
            try:
                body_data = json.loads(json_body)
                json_str = json.dumps(body_data)
            except json.JSONDecodeError as e:
                output_applier.apply([None, Pulse(), "Invalid JSON", 0, f"JSON parse error: {str(e)}"])
                printer.print(f"ERROR: Invalid JSON - {str(e)}")
                return
            
            # WebClient 생성 및 설정
            client = WebClient()
            client.Encoding = Encoding.UTF8
            client.Headers['Content-Type'] = 'application/json'
            
            # POST 요청
            response = client.UploadString(url, json_str)
            
            printer.print(f"✓ Success")
            printer.print(f"Response: {response[:200]}")
            
            output_applier.apply([Pulse(), None, response, 200, ""])
            
        except Exception as e:
            error_msg = f"Request failed: {str(e)}"
            output_applier.apply([None, Pulse(), "Error", 0, error_msg])
            printer.print(f"ERROR: {error_msg}")