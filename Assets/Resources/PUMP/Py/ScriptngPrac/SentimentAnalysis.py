# ===============================================================
# Sentiment Analysis Node - Text Emotion Detection
# Uses Microsoft Cognitive Services Text Analytics API
# ===============================================================

# .NET Framework references for HTTP requests
add_reference('System')

import System
from System.Net import WebClient, WebHeaderCollection
from System.Text import Encoding

# <<Node Configuration>>

# Node name displayed in visual editor
name: str = "Sentiment Analysis"

# Input: text to analyze, endpoint, API key for authentication
input_list: list = ['text', 'endpoint', 'api_key']

# Output: sentiment score (-1.0 to 1.0), confidence (0.0 to 1.0), emotion label
output_list: list = ['score', 'confidence', 'emotion']

# Input types: all text inputs
input_types: list = [str, str, str]

# Output types: score and confidence as float, emotion as string
output_types: list = [float, float, str]

# Enable async for HTTP API calls
is_async: bool = True

# System controllers
output_applier: OutputApplier = None
printer: Printer = None

json_util: JsonUtil = JsonUtil()

# Global variables for caching
_cached_api_key = None
_last_analysis_result = None

def init(inputs: list) -> None:
    """
    Initialize with neutral sentiment state and process any existing inputs
    """
    # Check if inputs are already connected and valid
    if inputs and len(inputs) >= 3:
        text_input = inputs[0]
        endpoint = inputs[1]
        api_key = inputs[2]
        
        # If all inputs are valid, perform analysis
        if (text_input is not None and endpoint is not None and api_key is not None and
            text_input.strip() != "" and endpoint.strip() != ""):
            
            try:
                result = analyze_sentiment(text_input, endpoint, api_key)
                if result is not None:
                    score, confidence, emotion = result
                    outputs = [score, confidence, emotion]
                    output_applier.apply(outputs)
                    printer.print(f"Init Analysis: {emotion} ({score:.2f}, {confidence:.2f})")
                    return
            except Exception as e:
                printer.print(f"Init Error: {str(e)}")
    
    # Default: Start with neutral values
    outputs = [0.0, 0.0, "neutral"]
    output_applier.apply(outputs)
    
    printer.print("Sentiment Analysis Ready")

def terminate() -> None:
    """
    Cleanup resources
    """
    global _cached_api_key, _last_analysis_result
    _cached_api_key = None
    _last_analysis_result = None
    return

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    Perform sentiment analysis when text, endpoint or API key changes
    """
    global _cached_api_key, _last_analysis_result
    
    # Only process if something actually changed
    if not is_changed:
        return
    
    text_input = inputs[0]
    endpoint = inputs[1]
    api_key = inputs[2]
    
    # Check if inputs are valid
    if text_input is None or endpoint is None or api_key is None:
        # Signal loss - clear outputs
        output_applier.apply([None, None, None])
        printer.print("Missing input: text, endpoint or API key")
        return
    
    if text_input.strip() == "":
        # Empty text - return neutral
        outputs = [0.0, 0.0, "neutral"]
        output_applier.apply(outputs)
        printer.print("Empty text - neutral result")
        return
    
    try:
        # Cache API key for reuse
        _cached_api_key = api_key
        
        # Perform sentiment analysis
        result = analyze_sentiment(text_input, endpoint, api_key)
        _last_analysis_result = result
        
        if result is not None:
            score, confidence, emotion = result
            outputs = [score, confidence, emotion]
            output_applier.apply(outputs)
            
            printer.print(f"Analysis: {emotion} ({score:.2f}, {confidence:.2f})")
        else:
            # API call failed
            output_applier.apply([None, None, None])
            printer.print("API call failed")
            
    except Exception as e:
        # Error occurred - signal loss
        output_applier.apply([None, None, None])
        printer.print(f"Error: {str(e)}")

def analyze_sentiment(text, endpoint, api_key):
    """
    Call Microsoft Cognitive Services Text Analytics API for sentiment analysis
    Returns: (score, confidence, emotion_label) or None if failed
    """
    try:
        # Use provided endpoint and append path
        if not endpoint.endswith('/'):
            endpoint += '/'
        path = "text/analytics/v3.1/sentiment"
        url = endpoint + path
        
        # Prepare request payload using Python dictionary
        documents = {
            "documents": [
                {
                    "id": "1",
                    "language": "en",
                    "text": text
                }
            ]
        }
        
        # Serialize to JSON using json_util
        json_data = json_util.serialize(documents)
        
        # Create web client for HTTP request
        client = WebClient()
        
        # Set headers
        client.Headers.Add("Ocp-Apim-Subscription-Key", api_key)
        client.Headers.Add("Content-Type", "application/json")
        
        # Make POST request
        response_bytes = client.UploadData(url, "POST", Encoding.UTF8.GetBytes(json_data))
        response_text = Encoding.UTF8.GetString(response_bytes)
        
        # Parse response using json_util
        response_data = json_util.deserialize(response_text)
        
        # Extract sentiment data
        documents_list = response_data["documents"]
        if documents_list and len(documents_list) > 0:
            doc_sentiment = documents_list[0]
            
            # Extract sentiment information
            sentiment_label = doc_sentiment["sentiment"]
            confidence_scores = doc_sentiment["confidenceScores"]
            
            # Convert sentiment to numeric score (-1 to 1)
            if sentiment_label == "positive":
                score = float(confidence_scores["positive"])
            elif sentiment_label == "negative":
                score = -float(confidence_scores["negative"])
            else:  # neutral
                score = 0.0
            
            # Overall confidence (highest confidence score)
            confidence = max(
                float(confidence_scores["positive"]),
                float(confidence_scores["negative"]),
                float(confidence_scores["neutral"])
            )
            
            return (score, confidence, sentiment_label)
        
        return None
        
    except Exception as e:
        # Log error for debugging
        printer.print(f"API Error: {str(e)}")
        return None

# Alternative implementation using a simpler sentiment analysis
def simple_sentiment_analysis(text):
    """
    Simple keyword-based sentiment analysis (fallback method)
    Returns: (score, confidence, emotion_label)
    """
    positive_words = ["good", "great", "excellent", "amazing", "wonderful", "fantastic", 
                     "love", "like", "happy", "joy", "best", "awesome", "perfect"]
    negative_words = ["bad", "terrible", "awful", "horrible", "hate", "dislike", 
                     "sad", "angry", "worst", "disgusting", "boring", "annoying"]
    
    text_lower = text.lower()
    words = text_lower.split()
    
    positive_count = sum(1 for word in words if word in positive_words)
    negative_count = sum(1 for word in words if word in negative_words)
    
    total_sentiment_words = positive_count + negative_count
    
    if total_sentiment_words == 0:
        return (0.0, 0.1, "neutral")
    
    score = (positive_count - negative_count) / len(words)
    confidence = min(total_sentiment_words / len(words), 1.0)
    
    if score > 0.1:
        emotion = "positive"
    elif score < -0.1:
        emotion = "negative"
    else:
        emotion = "neutral"
    
    return (score, confidence, emotion)