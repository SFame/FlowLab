# Simple Data Processor - Easy example using all types and parameters
name: str = "Data Processor"

# Input port setup using all supported types
input_list: list = ['Enable', 'Value', 'Multiplier', 'Label']

# Output port setup using all supported types  
output_list: list = ['Result', 'Is_Active']

# Input types: bool(control), float(value), int(multiplier), str(label)
input_types: list = [bool, float, int, str]

# Output types: float(result), bool(status)
output_types: list = [float, bool]

# Enable async execution - to demonstrate processing steps
is_async: bool = True

# System-provided objects
output_applier: OutputApplier = None
printer: Printer = None

# State management variables
current_label = "Default"
process_count = 0

def init(inputs: list) -> None:
    """
    Initialization function - using all inputs to set initial state
    inputs: [Enable(bool), Value(float), Multiplier(int), Label(str)]
    """
    global current_label, process_count
    
    # Individual assignment to explicitly show each input type
    enable_signal = inputs[0]    # bool type - True/False control signal
    input_value = inputs[1]      # float type - real number to process
    multiplier = inputs[2]       # int type - integer multiplier
    label_text = inputs[3]       # str type - text label
    
    # Set initial state
    current_label = label_text
    process_count = 0
    
    # Perform initial calculation
    if enable_signal:
        result = input_value * float(multiplier)  # Convert int to float for calculation
        printer.print(f"[{current_label}] Init: {input_value} × {multiplier} = {result}")
    else:
        result = 0.0
        printer.print(f"[{current_label}] Disabled at initialization")
    
    # Set initial output: [result value, activation status]
    output_applier.apply([result, enable_signal])

def terminate() -> None:
    """
    Cleanup function - simple cleanup tasks
    """
    global process_count
    printer.print(f"Processor terminated. Total processed: {process_count}")

def state_update(inputs: list, index: int, state, is_changed: bool, is_disconnected: bool) -> None:
    """
    State update function - detailed processing using all parameters
    
    inputs: Current state of all inputs [Enable, Value, Multiplier, Label]
    index: Index of port where change occurred (0:Enable, 1:Value, 2:Multiplier, 3:Label, -1:auto-call)
    state: New value of changed port (type depends on port: bool/float/int/str)
    is_changed: Whether value actually changed (True: changed, False: same value)
    is_disconnected: Whether change was due to disconnection (True: disconnected)
    """
    global current_label, process_count
    
    # Extract each input value by type
    enable_signal = inputs[0]    # bool
    input_value = inputs[1]      # float  
    multiplier = inputs[2]       # int
    label_text = inputs[3]       # str
    
    # Detailed processing by parameter - showing actual usage examples
    
    # 1. Using index parameter: identify which port changed
    port_names = ['Enable', 'Value', 'Multiplier', 'Label']
    if index == -1:
        printer.print("Auto-triggered after initialization")
    elif index >= 0 and index < len(port_names):
        printer.print(f"Changed port: {port_names[index]} = {state}")
    
    # 2. Using is_changed parameter: prevent unnecessary processing
    if not is_changed:
        printer.print("No actual change detected, skipping processing")
        return
    
    # 3. Using is_disconnected parameter: handle connection state
    if is_disconnected:
        printer.print("Port disconnected - using default values")
        # Use default values for disconnected ports
        if index == 0:  # Enable port disconnected
            enable_signal = False
        elif index == 1:  # Value port disconnected
            input_value = 0.0
        elif index == 2:  # Multiplier port disconnected
            multiplier = 1
        elif index == 3:  # Label port disconnected
            label_text = "Disconnected"
    
    # 4. Using state parameter: type-specific handling of changed values
    if index == 0:  # Enable port (bool type)
        printer.print(f"Enable changed to: {state} (bool type)")
    elif index == 1:  # Value port (float type)
        printer.print(f"Value changed to: {state:.2f} (float type)")
    elif index == 2:  # Multiplier port (int type)
        printer.print(f"Multiplier changed to: {state} (int type)")
    elif index == 3:  # Label port (str type)
        printer.print(f"Label changed to: '{state}' (str type)")
        current_label = state  # Update label
    
    # Main processing logic
    if enable_signal:
        # Perform calculation when enabled
        result = input_value * float(multiplier)  # Explicit type conversion
        process_count += 1
        
        printer.print(f"[{current_label}] Processing #{process_count}: {input_value} × {multiplier} = {result}")
        
        # Output result: [calculated result, active status]
        output_applier.apply([result, True])
        
    else:
        # When disabled
        printer.print(f"[{current_label}] Disabled - output set to 0")
        
        # Disabled output: [0, inactive status]
        output_applier.apply([0.0, False])
    
    # Show additional info - current state summary
    printer.print(f"Current state: Enable={enable_signal}, Value={input_value:.1f}, Mult={multiplier}, Label='{label_text}'")