add_reference('System')
from System import Environment
from System.IO import Path, File

# 바탕화면에 텍스트 파일을 저장하는 노드
name: str = "File Saver"

# 입력 포트: 저장할 텍스트 값과 저장 트리거
input_list: list = ['value', 'save']

# 출력 포트 없음 (파일 저장만 수행)
output_list: list = []

# 입력 타입: 문자열 값과 불린 트리거
input_types: list = [str, bool]

# 출력 타입 없음
output_types: list = []

# 파일 저장은 간단한 작업이므로 비동기 불필요
is_async: bool = False

# 시스템 제공 객체들
output_applier: OutputApplier = None
printer: Printer = None

# 저장할 텍스트 내용
current_text = ""
file_counter = 1

def init(inputs: list) -> None:
    """
    노드 초기화 - 현재 입력된 텍스트 저장
    inputs: [value(str), save(bool)]
    """
    global current_text
    
    # 초기 텍스트 값 저장
    current_text = inputs[0]
    
    printer.print(f"File Saver ready - Text: '{current_text[:20]}{'...' if len(current_text) > 20 else ''}'")

def terminate() -> None:
    """
    정리 함수 - 특별한 리소스 정리 불필요
    """
    printer.print("File Saver terminated")

def state_update(inputs: list, index: int, state, is_changed: bool, is_disconnected: bool) -> None:
    """
    입력 변화 처리 함수
    
    inputs: [value(str), save(bool)]
    index: 변화가 발생한 포트 (0: value, 1: save)
    state: 변화된 포트의 새 값
    is_changed: 실제 값이 변했는지 여부
    is_disconnected: 연결 해제로 인한 변화인지
    """
    global current_text, file_counter
    
    # 실제 변화가 없으면 무시
    if not is_changed:
        return
    
    # 연결 해제 시 기본값 사용
    if is_disconnected:
        if index == 0:  # value 포트 연결 해제
            printer.print("Text input disconnected")
            return
        elif index == 1:  # save 포트 연결 해제
            printer.print("Save trigger disconnected")
            return
    
    # 포트별 처리
    if index == 0:  # value 포트 (문자열 입력)
        current_text = state
        printer.print(f"Text updated: '{current_text[:30]}{'...' if len(current_text) > 30 else ''}'")
        
    elif index == 1 and state:  # save 포트 (True로 변할 때만)
        save_to_desktop()

def save_to_desktop():
    """
    현재 텍스트를 바탕화면에 파일로 저장
    """
    global current_text, file_counter
    
    try:
        # 바탕화면 경로 가져오기 (.NET Framework 사용)
        desktop_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        
        # 파일명 생성 (중복 방지를 위해 카운터 사용)
        filename = f"saved_text_{file_counter:03d}.txt"
        full_path = Path.Combine(desktop_path, filename)
        
        # 파일명이 이미 존재하면 카운터 증가
        while File.Exists(full_path):
            file_counter += 1
            filename = f"saved_text_{file_counter:03d}.txt"
            full_path = Path.Combine(desktop_path, filename)
        
        # 파일에 텍스트 저장 (.NET Framework File.WriteAllText 사용)
        File.WriteAllText(full_path, current_text)
        
        # 성공 메시지 표시
        printer.print(f"✓ Saved: {filename}")
        printer.print(f"Path: {desktop_path}")
        
        # 다음 저장을 위해 카운터 증가
        file_counter += 1
        
    except Exception as e:
        # 에러 발생 시 메시지 표시
        printer.print(f"✗ Save failed: {str(e)}")
        printer.print("Check permissions and try again")
