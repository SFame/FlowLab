# ===============================================================
# Sentence Generator Node - Sequential Sentence Output
# Outputs predefined sentences for sentiment analysis testing
# ===============================================================

# <<Node Configuration>>

# Node name displayed in visual editor
name: str = "Sentence Generator"

# Input: trigger signal to get next sentence, reset signal to go back to first
input_list: list = ['trigger', 'reset']

# Output: English sentence for sentiment analysis
output_list: list = ['sentence']

# Input types: both boolean
input_types: list = [bool, bool]

# Output types: string sentence
output_types: list = [str]

# Synchronous execution for simple logic
is_async: bool = False

# System controllers
output_applier: OutputApplier = None
printer: Printer = None

# Predefined sentences for sentiment analysis (no neutral sentences)
_sentences = [
    "I love this beautiful sunny day!",
    "This movie was absolutely terrible and boring.",
    "The food at this restaurant is amazing and delicious!",
    "I hate waiting in these ridiculously long lines.",
    "What a wonderful surprise this was for me!",
    "This software is buggy, slow and completely frustrating.",
    "I'm so excited about my vacation next week!",
    "This book changed my life in the most incredible way.",
    "I'm deeply disappointed with the poor customer service.",
    "The concert last night was absolutely incredible!",
    "This traffic jam is making me extremely angry.",
    "I feel so grateful for my amazing family and friends.",
    "The new update completely broke my favorite feature.",
    "This coffee tastes fantastic and perfect this morning!",
    "I'm really worried and stressed about the upcoming exam.",
    "The sunset view from here is absolutely breathtaking.",
    "This meeting was a complete waste of everyone's time.",
    "I'm thrilled and excited to announce my promotion!",
    "The internet connection is painfully slow and unreliable today.",
    "This pizza is the best I've ever tasted in my life!",
    "I'm feeling overwhelmed and stressed about my heavy workload.",
    "The customer support team was incredibly helpful and kind.",
    "This game is addictive, fun and brilliantly designed!",
    "I'm sad and heartbroken that the vacation is already over.",
    "The new restaurant downtown is excellent and outstanding!",
    "This impossible project deadline is causing me serious stress.",
    "I deeply appreciate all the wonderful help you've given me.",
    "The user interface is confusing, ugly and poorly designed.",
    "I'm disgusted by how rude and unprofessional they were.",
    "This success has made me incredibly happy and proud!"
]

# Current sentence index
_current_index = 0

def init(inputs: list) -> None:
    """
    Initialize and output the first sentence
    """
    global _current_index
    _current_index = 0
    
    # Output the first sentence
    first_sentence = _sentences[_current_index]
    output_applier.apply([first_sentence])
    
    printer.print(f"Initialized with sentence {_current_index}: {first_sentence[:50]}...")

def terminate() -> None:
    """
    Cleanup function - reset index
    """
    global _current_index
    _current_index = 0
    return

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    Generate next sentence when trigger becomes True, reset to beginning when reset becomes True
    """
    global _current_index
    
    # Only process if something actually changed
    if not is_changed:
        return
    
    trigger = inputs[0]
    reset = inputs[1]
    
    # Always output current sentence, even if inputs are None
    current_sentence = _sentences[_current_index]
    
    # If inputs are None, just maintain current sentence
    if trigger is None or reset is None:
        output_applier.apply([current_sentence])
        printer.print("Signal loss detected - maintaining current sentence")
        return
    
    # Priority: Reset first, then trigger
    # Reset to beginning when reset becomes True
    if reset == True and before_state != True:
        _current_index = 0
        current_sentence = _sentences[_current_index]
        output_applier.apply([current_sentence])
        printer.print(f"Reset to sentence 0: {current_sentence[:50]}...")
        return
    
    # Advance to next sentence when trigger becomes True
    if trigger == True and (index == 0 and before_state != True):  # Only respond to trigger port changes
        # Move to next sentence
        _current_index = (_current_index + 1) % len(_sentences)
        
        # Get current sentence
        current_sentence = _sentences[_current_index]
        
        # Output the sentence
        output_applier.apply([current_sentence])
        
        printer.print(f"Generated sentence {_current_index}: {current_sentence[:50]}...")
        return
    
    # For other cases, maintain current sentence
    output_applier.apply([current_sentence])

def get_sentence_count():
    """
    Utility function to get total number of sentences
    Returns: int - total sentence count
    """
    return len(_sentences)

def get_current_index():
    """
    Utility function to get current sentence index
    Returns: int - current index
    """
    global _current_index
    return _current_index

def reset_to_beginning():
    """
    Utility function to reset to first sentence
    """
    global _current_index
    _current_index = 0
    first_sentence = _sentences[_current_index]
    output_applier.apply([first_sentence])
    printer.print("Reset to first sentence")