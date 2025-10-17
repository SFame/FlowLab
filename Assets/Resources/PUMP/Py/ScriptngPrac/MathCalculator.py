# ===============================================================
# IronPython Version: 3.4.2 (3.4.2.1000)
# ===============================================================

# <<Node Configuration>>

name: str = "Math Calculator"

input_list: list = ['Execute', 'Number A', 'Number B', 'Operation']
output_list: list = ['Result', 'IsValid', 'Message']

input_types: list = [Pulse, float, float, str]
output_types: list = [float, bool, str]

is_async: bool = False

# <<Node Controllers>>
output_applier: OutputApplier = None
printer: Printer = None

# <<Utilities>>
json_util: JsonUtil = JsonUtil()

# <<Global Variables>>
last_result = 0.0
operation_count = 0

# <<Node Lifecycle Methods>>

def init(inputs: list) -> None:
    """초기화"""
    global last_result, operation_count
    last_result = 0.0
    operation_count = 0
    
    output_applier.apply([0.0, True, "Ready"])
    
    if printer is not None:
        printer.print("🧮 Calculator Ready")

def terminate() -> None:
    """정리"""
    if printer is not None:
        printer.print(f"🧮 Calculator terminated ({operation_count} operations)")

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """계산 실행"""
    global last_result, operation_count
    
    # Execute 펄스 신호가 왔을 때만 계산
    if index == 0:
        if inputs[1] is None or inputs[2] is None or inputs[3] is None:
            output_applier.apply([0.0, False, "Missing inputs"])
            if printer is not None:
                printer.print("⚠️ Missing inputs")
            return
        
        num_a = float(inputs[1])
        num_b = float(inputs[2])
        operation = str(inputs[3]).strip().lower()
        
        try:
            result = 0.0
            
            if operation == "add" or operation == "+":
                result = num_a + num_b
            elif operation == "subtract" or operation == "-":
                result = num_a - num_b
            elif operation == "multiply" or operation == "*":
                result = num_a * num_b
            elif operation == "divide" or operation == "/":
                if num_b == 0:
                    output_applier.apply([0.0, False, "Division by zero"])
                    if printer is not None:
                        printer.print("⚠️ Division by zero!")
                    return
                result = num_a / num_b
            else:
                output_applier.apply([0.0, False, f"Unknown operation: {operation}"])
                if printer is not None:
                    printer.print(f"⚠️ Unknown operation: {operation}")
                return
            
            last_result = result
            operation_count += 1
            
            output_applier.apply([result, True, f"{num_a} {operation} {num_b} = {result}"])
            
            if printer is not None:
                printer.print(f"✓ {num_a} {operation} {num_b} = {result}")
                
        except Exception as e:
            output_applier.apply([0.0, False, f"Error: {str(e)}"])
            if printer is not None:
                printer.print(f"✗ Error: {str(e)}")