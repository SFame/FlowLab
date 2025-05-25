# Node name configuration - Defines the name displayed in the visual editor
# Use a clear name that represents the logic gate's function
name: str = "AND Gate"

# Input port definition - AND gate requires 2 inputs, so set 2 ports
# Named 'A', 'B' following standard logic gate notation
input_list: list = ['A', 'B']

# Output port definition - AND gate produces only 1 output, so set 1 port
# Named 'Y' following logic gate notation (result output)
output_list: list = ['Y']

# Input type configuration - All inputs are bool type (True/False) for logic gate
# Must match the length of input_list (type specified for each port)
input_types: list = [bool, bool]

# Output type configuration - Logic operation result is also bool type
# Must match the length of output_list
output_types: list = [bool]

# Asynchronous execution setting - Set to False because:
# AND gate is a simple logic operation that computes instantly, no async needed
# Synchronous execution ensures fast response
is_async: bool = False

# System-managed output controller object
# Used to send calculated results to output ports
# Never initialize this directly - system sets it automatically
output_applier: OutputApplier = None

# System-managed display object
# Not used in this example but useful for debugging
# Never initialize this directly - system sets it automatically
printer: Printer = None

def init(inputs: list) -> None:
    """
    Node initialization function - executed once when node is created
    
    Why set initial output here?
    - To show correct output based on current input state right after node creation
    - Allows users to see current state immediately after adding the node
    
    inputs: Current state of input ports [A, B]
    """
    # Why use all() function:
    # - Simple implementation of AND gate logic (True only when all inputs are True)
    # - Works consistently even if number of inputs changes
    # - Optimized built-in function
    result = all(inputs)
    
    # Why create output as a list:
    # - output_applier.apply() requires a list
    # - Consistent interface for cases with multiple output ports
    outputs = [result]
    
    # Apply calculated result to actual output port
    # This is how signals are transmitted to other connected nodes in visual editor
    output_applier.apply(outputs)

def terminate() -> None:
    """
    Node cleanup function - executed when node is deleted
    
    AND gate doesn't store state or use resources, so no special cleanup needed
    - Function must still be defined even if empty
    """
    return

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    Input change detection and processing function - where core logic is implemented
    
    Why check is_changed?
    - Ignore signals that haven't actually changed to prevent unnecessary calculations
    - Performance optimization and infinite loop prevention
    
    inputs: Current state of all input ports [A, B]
    index: Index of port where change occurred (0: A, 1: B)
    state: New value of the changed port
    is_changed: Whether the value is actually different from previous
    """
    # Don't process if value hasn't actually changed
    # This is important optimization - prevents processing repeated identical signals
    if not is_changed:
        return
    
    # Core AND gate logic implementation
    # Why use all() function:
    # - Python built-in function that's optimized for performance
    # - Code is clear and easy to understand
    # - Works the same way regardless of number of inputs
    result = all(inputs)
    
    # Why structure output as a list:
    # - Required by output_applier.apply() interface
    # - Maintains consistent code structure with multi-output nodes
    # - Must match order and length of output_types list
    outputs = [result]
    
    # Apply final result to output port
    # This call triggers state_update in other connected nodes
    output_applier.apply(outputs)