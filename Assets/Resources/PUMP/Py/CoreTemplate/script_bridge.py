import clr

reference_ex_logger = None

def add_reference(reference: str) -> None:
    try:
        clr.AddReference(reference)
    except:
        if reference_ex_logger is not None:
            reference_ex_logger(reference)


class Pulse:
    """A marker class to identify Pulse-type execution ports."""
    _instance = None
    def __new__(cls, *args, **kwargs):
        if not cls._instance:
            cls._instance = super().__new__(cls, *args, **kwargs)
        
        return cls._instance

def get_pulse_instance():
    return Pulse()


class OutputApplier:
    def __init__(self):
        self.callback_apply = None
        self.callback_apply_at = None
        self.callback_apply_to = None

    def set_callback(self, callback_apply, callback_apply_at, callback_apply_to) -> None:
        self.callback_apply = callback_apply
        self.callback_apply_at = callback_apply_at
        self.callback_apply_to = callback_apply_to
    
    def dispose(self) -> None:
        self.callback_apply = None
        self.callback_apply_at = None
        self.callback_apply_to = None

    def apply(self, values: list) -> None:
        if self.callback_apply is None:
            return
        self.callback_apply(values)

    def apply_at(self, index: int, value) -> None:
        if self.callback_apply_at is None:
            return
        self.callback_apply_at(index, value)

    def apply_to(self, name: str, value) -> None:
        if self.callback_apply_to is None:
            return
        self.callback_apply_to(name, value)


class Printer:
    def __init__(self) -> None:
        self.callback_print = None

    def set_callback(self, callback_print) -> None:
        self.callback_print = callback_print

    def dispose(self) -> None:
        self.callback_print = None

    def print(self, value: str) -> None:
        if self.callback_print is None:
            return
        self.callback_print(value)


class JsonUtil:
    def serialize(self, data, pretty: bool = False) -> str:
        return self._serialize_object(data, 0 if not pretty else 0, pretty)
    
    def deserialize(self, json_text: str):
        return self._parse_json(json_text.strip())
    
    def try_serialize(self, data, pretty: bool = False) -> tuple:
        try:
            result = self.serialize(data, pretty)
            return (True, result)
        except Exception as e:
            return (False, str(e))
    
    def try_deserialize(self, json_text: str) -> tuple:
        try:
            result = self.deserialize(json_text)
            return (True, result)
        except Exception as e:
            return (False, str(e))
    
    def is_valid(self, json_text: str) -> bool:
        success, _ = self.try_deserialize(json_text)
        return success
    
    def _serialize_object(self, obj, indent_level: int, pretty: bool) -> str:
        if obj is None:
            return "null"
        elif isinstance(obj, bool):
            return "true" if obj else "false"
        elif isinstance(obj, int):
            return str(obj)
        elif isinstance(obj, float):
            if obj != obj:
                return "null"
            elif obj == float('inf'):
                return "null"
            elif obj == float('-inf'):
                return "null"
            else:
                return str(obj)
        elif isinstance(obj, str):
            return self._escape_string(obj)
        elif isinstance(obj, (list, tuple)):
            return self._serialize_array(obj, indent_level, pretty)
        elif isinstance(obj, dict):
            return self._serialize_dict(obj, indent_level, pretty)
        else:
            return self._serialize_dotnet_object(obj, indent_level, pretty)
    
    def _escape_string(self, s: str) -> str:
        escape_map = {
            '"': '\\"',
            '\\': '\\\\',
            '\b': '\\b',
            '\f': '\\f',
            '\n': '\\n',
            '\r': '\\r',
            '\t': '\\t'
        }
        
        result = '"'
        for char in s:
            if char in escape_map:
                result += escape_map[char]
            elif ord(char) < 32:
                result += f'\\u{ord(char):04x}'
            else:
                result += char
        result += '"'
        return result
    
    def _serialize_array(self, arr, indent_level: int, pretty: bool) -> str:
        if not arr:
            return "[]"
        
        items = []
        for item in arr:
            items.append(self._serialize_object(item, indent_level + 1, pretty))
        
        if pretty:
            indent = "  " * (indent_level + 1)
            end_indent = "  " * indent_level
            return "[\n" + indent + f",\n{indent}".join(items) + "\n" + end_indent + "]"
        else:
            return "[" + ",".join(items) + "]"
    
    def _serialize_dict(self, d, indent_level: int, pretty: bool) -> str:
        if not d:
            return "{}"
        
        pairs = []
        for key, value in d.items():
            key_str = self._escape_string(str(key))
            value_str = self._serialize_object(value, indent_level + 1, pretty)
            pairs.append(f"{key_str}:{' ' if pretty else ''}{value_str}")
        
        if pretty:
            indent = "  " * (indent_level + 1)
            end_indent = "  " * indent_level
            return "{\n" + indent + f",\n{indent}".join(pairs) + "\n" + end_indent + "}"
        else:
            return "{" + ",".join(pairs) + "}"
    
    def _serialize_dotnet_object(self, obj, indent_level: int, pretty: bool) -> str:
        add_reference('System')
        
        obj_type = type(obj).__name__
        
        if obj_type == 'Boolean':
            return "true" if bool(obj) else "false"
        elif obj_type in ['Int32', 'Int64', 'Byte', 'SByte', 'Int16', 'UInt16', 'UInt32', 'UInt64']:
            return str(int(obj))
        elif obj_type in ['Single', 'Double', 'Decimal']:
            val = float(obj)
            if val != val or val == float('inf') or val == float('-inf'):
                return "null"
            return str(val)
        elif obj_type == 'String':
            return self._escape_string(str(obj))
        elif hasattr(obj, 'Keys') and hasattr(obj, 'Values'):
            d = {}
            for key in obj.Keys:
                d[str(key)] = obj[key]
            return self._serialize_dict(d, indent_level, pretty)
        elif hasattr(obj, '__iter__') and not isinstance(obj, str):
            try:
                arr = list(obj)
                return self._serialize_array(arr, indent_level, pretty)
            except:
                pass
        
        return self._escape_string(str(obj))
    
    def _parse_json(self, json_str: str):
        self._pos = 0
        self._json = json_str
        self._length = len(json_str)
        
        result = self._parse_value()
        self._skip_whitespace()
        
        if self._pos < self._length:
            raise ValueError(f"Unexpected character at position {self._pos}: '{self._json[self._pos]}'")
        
        return result
    
    def _parse_value(self):
        self._skip_whitespace()
        
        if self._pos >= self._length:
            raise ValueError("Unexpected end of JSON input")
        
        char = self._json[self._pos]
        
        if char == '"':
            return self._parse_string()
        elif char == '{':
            return self._parse_object()
        elif char == '[':
            return self._parse_array()
        elif char == 't':
            return self._parse_literal('true', True)
        elif char == 'f':
            return self._parse_literal('false', False)
        elif char == 'n':
            return self._parse_literal('null', None)
        elif char == '-' or char.isdigit():
            return self._parse_number()
        else:
            raise ValueError(f"Unexpected character: '{char}' at position {self._pos}")
    
    def _parse_string(self) -> str:
        if self._json[self._pos] != '"':
            raise ValueError(f"Expected '\"' at position {self._pos}")
        
        self._pos += 1
        start = self._pos
        result = ""
        
        while self._pos < self._length:
            char = self._json[self._pos]
            
            if char == '"':
                result += self._json[start:self._pos]
                self._pos += 1
                return result
            elif char == '\\':
                result += self._json[start:self._pos]
                self._pos += 1
                if self._pos >= self._length:
                    raise ValueError("Unterminated string escape")
                
                escape_char = self._json[self._pos]
                if escape_char == '"':
                    result += '"'
                elif escape_char == '\\':
                    result += '\\'
                elif escape_char == '/':
                    result += '/'
                elif escape_char == 'b':
                    result += '\b'
                elif escape_char == 'f':
                    result += '\f'
                elif escape_char == 'n':
                    result += '\n'
                elif escape_char == 'r':
                    result += '\r'
                elif escape_char == 't':
                    result += '\t'
                elif escape_char == 'u':

                    if self._pos + 4 >= self._length:
                        raise ValueError("Incomplete unicode escape")
                    hex_digits = self._json[self._pos + 1:self._pos + 5]
                    try:
                        code_point = int(hex_digits, 16)
                        result += chr(code_point)
                        self._pos += 4
                    except ValueError:
                        raise ValueError(f"Invalid unicode escape: \\u{hex_digits}")
                else:
                    raise ValueError(f"Invalid escape character: \\{escape_char}")
                
                self._pos += 1
                start = self._pos
            else:
                self._pos += 1
        
        raise ValueError("Unterminated string")
    
    def _parse_object(self) -> dict:
        """객체 파싱"""
        if self._json[self._pos] != '{':
            raise ValueError(f"Expected '{{' at position {self._pos}")
        
        self._pos += 1
        result = {}
        self._skip_whitespace()
        
        if self._pos < self._length and self._json[self._pos] == '}':
            self._pos += 1
            return result
        
        while True:
            self._skip_whitespace()
            
            if self._pos >= self._length or self._json[self._pos] != '"':
                raise ValueError(f"Expected string key at position {self._pos}")
            
            key = self._parse_string()
            self._skip_whitespace()
            
            if self._pos >= self._length or self._json[self._pos] != ':':
                raise ValueError(f"Expected ':' at position {self._pos}")
            
            self._pos += 1
            
            value = self._parse_value()
            result[key] = value
            
            self._skip_whitespace()
            
            if self._pos >= self._length:
                raise ValueError("Unterminated object")
            
            char = self._json[self._pos]
            if char == '}':
                self._pos += 1
                break
            elif char == ',':
                self._pos += 1
            else:
                raise ValueError(f"Expected ',' or '}}' at position {self._pos}")
        
        return result
    
    def _parse_array(self) -> list:
        if self._json[self._pos] != '[':
            raise ValueError(f"Expected '[' at position {self._pos}")
        
        self._pos += 1
        result = []
        self._skip_whitespace()
        
        if self._pos < self._length and self._json[self._pos] == ']':
            self._pos += 1
            return result
        
        while True:
            value = self._parse_value()
            result.append(value)
            
            self._skip_whitespace()
            
            if self._pos >= self._length:
                raise ValueError("Unterminated array")
            
            char = self._json[self._pos]
            if char == ']':
                self._pos += 1
                break
            elif char == ',':
                self._pos += 1
            else:
                raise ValueError(f"Expected ',' or ']' at position {self._pos}")
        
        return result
    
    def _parse_literal(self, literal: str, value):
        if self._json[self._pos:self._pos + len(literal)] == literal:
            self._pos += len(literal)
            return value
        else:
            raise ValueError(f"Expected '{literal}' at position {self._pos}")
    
    def _parse_number(self):
        start = self._pos
        
        if self._json[self._pos] == '-':
            self._pos += 1
        
        if self._pos >= self._length or not self._json[self._pos].isdigit():
            raise ValueError(f"Invalid number at position {start}")
        
        if self._json[self._pos] == '0':
            self._pos += 1
        else:
            while self._pos < self._length and self._json[self._pos].isdigit():
                self._pos += 1
        
        is_float = False
        
        if self._pos < self._length and self._json[self._pos] == '.':
            is_float = True
            self._pos += 1
            if self._pos >= self._length or not self._json[self._pos].isdigit():
                raise ValueError(f"Invalid number at position {start}")
            while self._pos < self._length and self._json[self._pos].isdigit():
                self._pos += 1
        
        if self._pos < self._length and self._json[self._pos].lower() == 'e':
            is_float = True
            self._pos += 1
            if self._pos < self._length and self._json[self._pos] in '+-':
                self._pos += 1
            if self._pos >= self._length or not self._json[self._pos].isdigit():
                raise ValueError(f"Invalid number at position {start}")
            while self._pos < self._length and self._json[self._pos].isdigit():
                self._pos += 1
        
        number_str = self._json[start:self._pos]
        
        try:
            if is_float:
                return float(number_str)
            else:
                return int(number_str)
        except ValueError:
            raise ValueError(f"Invalid number: {number_str}")
    
    def _skip_whitespace(self):
        while self._pos < self._length and self._json[self._pos] in ' \t\n\r':
            self._pos += 1