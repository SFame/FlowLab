using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using OdinSerializer;
using SFB;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ArgumentNullException = System.ArgumentNullException;
using Object = UnityEngine.Object;
using SerializationUtility = OdinSerializer.SerializationUtility;

namespace Utils
{
    public static class Other // 짬통
    {
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static T GetComponentInSibling<T>(this Component component, bool includeInactive = true) where T : Component
        {
            if (component == null)
            {
                Debug.LogError("Target is Null");
                return null;
            }
            
            Transform parent = component.transform.parent;
            if (parent == null)
                return Object.FindFirstObjectByType<T>(findObjectsInactive: includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

            foreach (Transform child in parent)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy)
                    continue;
                
                if (child.TryGetComponent(out T result))
                    return result;
            }

            return null;
        }

        public static void SafeCancelAndDispose(this CancellationTokenSource cts)
        {
            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch { }

            cts.Dispose();
        }

        public static bool IsAlive<T>(this T component) where T : Component
        {
            return component != null && component.gameObject && !component.gameObject.IsDestroyed();
        }

        public static T[] GetResources<T>(params string[] pathStrings) where T : Object
        {
            return pathStrings.Select(Resources.Load<T>).ToArray();
        }

        public static async UniTask InvokeActionDelay(Action action , PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            await UniTask.Yield(timing);
            action?.Invoke();
        }


        public static async UniTask InvokeActionWaitTime(Action action, float delay, CancellationToken token = default)
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: token);
            action?.Invoke();
        }
        public static string AsString(this object obj)
        {
            return obj is string str
                ? str
                : throw new InvalidCastException("Object cannot be converted to string");
        }

        public static IEnumerable<T> WhereForeach<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                if (predicate?.Invoke(item) ?? false)
                {
                    action?.Invoke(item);
                }
            }

            return enumerable;
        }

        public static bool HasIntersection<T>(this IEnumerable<T> current, IEnumerable<T> target)
        {
            return current.Intersect(target).Any();
        }

        public static T FindUnderPoint<T>(this PointerEventData eventData)
        {
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(eventData, results);
            return results
                .Select(result => result.gameObject.GetComponent<T>())
                .FirstOrDefault(selected => !selected.IsUnityNull());
        }

        public static async UniTask LerpAction(float duration, Action<float> action, Action finalizer = null, CancellationToken token = default)
        {
            float elapsed = 0;

            try
            {
                while (elapsed < duration && !token.IsCancellationRequested)
                {
                    float t = elapsed / duration;
                    action?.Invoke(t);
                    await UniTask.Yield(token);
                    elapsed += Time.deltaTime;
                }

                if (!token.IsCancellationRequested)
                {
                    action?.Invoke(1.0f);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                finalizer?.Invoke();
            }
        }
        
        public static async UniTask CurveAction(this AnimationCurve curve, float duration, Action<float> action, Action finalizer = null, CancellationToken token = default)
        {
            float elapsed = 0;

            try
            {
                while (elapsed < duration && !token.IsCancellationRequested)
                {
                    float t = elapsed / duration;
                    action?.Invoke(curve.Evaluate(t));
                    await UniTask.Yield(token);
                    elapsed += Time.deltaTime;
                }

                if (!token.IsCancellationRequested)
                {
                    action?.Invoke(curve.Evaluate(1.0f));
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                finalizer?.Invoke();
            }
        }

        /// <summary>
        /// 문자열 꺾쇠괄호 제거
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveRichTextTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string pattern = @"<([a-zA-Z][a-zA-Z0-9]*)(?:[^<>]*)>(.*?)</\1>";
            string result = Regex.Replace(input, pattern, "$2");
            return result;
        }
    }

    public static class UtilsDebug
    {
        public static T Log<T>(this T logObject, LogType logType = LogType.Log)
        {
            Debug.unityLogger.Log(logType, logObject);
            return logObject;
        }

        /// <summary>
        /// Logging Enumerable elements
        /// </summary>
        /// <typeparam name="T">where : IEnumerable</typeparam>
        /// <param name="logEnumerable">Enumerable instance</param>
        /// <param name="logType">LogType</param>
        /// <returns></returns>
        public static IEnumerable<T> LogE<T>(this IEnumerable<T> logEnumerable, LogType logType = LogType.Log)
        {
            if (logEnumerable == null)
            {
                Debug.unityLogger.Log(logType, "Null");
                return logEnumerable;
            }

            StringBuilder sb = new();
            foreach (T obj in logEnumerable)
            {
                sb.AppendLine($"[{obj.ToString()}]");
            }
            Debug.unityLogger.Log(logType, sb);
            return logEnumerable;
        }

        /// <summary>
        /// Logging Enumerable elements
        /// </summary>
        /// <typeparam name="T">where : IEnumerable</typeparam>
        /// <param name="logEnumerable">Enumerable instance</param>
        /// <param name="logType">LogType</param>
        /// <returns></returns>
        public static IEnumerable<T> LogE<T>(this IEnumerable<T> logEnumerable, Func<T, object> logMap, LogType logType = LogType.Log)
        {
            logMap ??= (t => t);

            if (logEnumerable == null)
            {
                Debug.unityLogger.Log(logType, "Null");
                return logEnumerable;
            }

            StringBuilder sb = new();
            foreach (T obj in logEnumerable)
            {
                sb.AppendLine($"[{logMap(obj).ToString()}]");
            }
            Debug.unityLogger.Log(logType, sb);
            return logEnumerable;
        }
    }

    public static class RaycasterUtil
    {
        public static List<RaycastResult> FindUnderPoint(this GraphicRaycaster raycaster, Vector2 point)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = point;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            return results.DistinctBy(r => r.gameObject).ToList();
        }

        public static List<RaycastResult> GridRaycast(this GraphicRaycaster raycaster, Vector2 startPos, Vector2 endPos, float gridSize = 10f)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);

            float minX = Mathf.Min(startPos.x, endPos.x);
            float maxX = Mathf.Max(startPos.x, endPos.x);
            float minY = Mathf.Min(startPos.y, endPos.y);
            float maxY = Mathf.Max(startPos.y, endPos.y);

            List<RaycastResult> results = new List<RaycastResult>();
            for (float x = minX; x <= maxX; x += gridSize)
            {
                for (float y = minY; y <= maxY; y += gridSize)
                {
                    pointerData.position = new Vector2(x, y);
                    raycaster.Raycast(pointerData, results);
                }
            }

            return results.DistinctBy(r => r.gameObject).ToList();
        }

        public static List<T> FindUnderPoint<T>(this GraphicRaycaster raycaster, Vector2 point)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = point;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            List<T> foundComponents = new List<T>();
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.TryGetComponent(out T component))
                    foundComponents.Add(component);
            }
            return foundComponents;
        }

        public static HashSet<T> GridRaycast<T>(this GraphicRaycaster raycaster, Vector2 startPos, Vector2 endPos, float gridSize = 10f)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);

            float minX = Mathf.Min(startPos.x, endPos.x);
            float maxX = Mathf.Max(startPos.x, endPos.x);
            float minY = Mathf.Min(startPos.y, endPos.y);
            float maxY = Mathf.Max(startPos.y, endPos.y);

            List<RaycastResult> results = new List<RaycastResult>();
            for (float x = minX; x <= maxX; x += gridSize)
            {
                for (float y = minY; y <= maxY; y += gridSize)
                {
                    pointerData.position = new Vector2(x, y);
                    raycaster.Raycast(pointerData, results);
                }
            }

            HashSet<T> selectedObjects = new HashSet<T>();
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.TryGetComponent(out T component))
                    selectedObjects.Add(component);
            }
            return selectedObjects;
        }
    }

    public static class Capture
    {
        private static Vector2 ScreenSize => new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

        /// <summary>
        /// targetRect(및 자식)만 캡처하여 size 크기의 Texture2D로 반환
        /// </summary>
        public static Texture2D CaptureRect(this RectTransform targetRect, Vector2 size, string captureLayerName = "ForCapture")
        {
            if (targetRect == null)
            {
                Debug.LogError("[CaptureRect] targetRect가 null입니다.");
                return null;
            }

            // 1) 캔버스 찾고, 원본 상태 백업
            Canvas canvas = targetRect.GetRootCanvas();
            if (canvas == null)
            {
                Debug.LogError("[CaptureRect] targetRect가 어떤 Canvas에도 속해있지 않습니다.");
                return null;
            }
            RenderMode originalRenderMode = canvas.renderMode;
            Camera originalWorldCamera = canvas.worldCamera;
            bool originalOverrideSorting = canvas.overrideSorting;
            int originalSortingOrder = canvas.sortingOrder;

            // 2) 레이어 변경 준비
            int captureLayer = LayerMask.NameToLayer(captureLayerName);
            if (captureLayer < 0)
            {
                Debug.LogError($"[CaptureRect] '{captureLayerName}' 레이어가 프로젝트에 없습니다. " +
                               $"Project Settings > Tags and Layers에서 미리 추가해 주세요.");
                return null;
            }
        
            // targetRect 및 자식들의 원래 레이어 백업 후, 캡처 레이어로 설정
            Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
            SetLayerRecursively(targetRect, captureLayer, originalLayers);

            Texture2D result = null;
            Camera captureCam = null;
            GameObject camObj = null;
            RenderTexture rt = null;

            try
            {
                // 3) 월드 좌표로 targetRect의 사각영역(바운딩 박스) 구하기
                Rect rectWorldBounds = GetWorldBoundsRect(targetRect);

                // 4) 모드별 분기
                switch (canvas.renderMode)
                {
                    case RenderMode.ScreenSpaceOverlay:
                        // (A) ScreenSpace-Overlay => 카메라가 없으므로, 
                        //     "일시적으로 ScreenSpace-Camera로 전환" → 캡처용 카메라 렌더 → 복원
                        camObj = new GameObject("TempUICamera_Overlay");
                        captureCam = camObj.AddComponent<Camera>();
                        captureCam.orthographic = true;
                        captureCam.cullingMask = 1 << captureLayer;

                        // Canvas를 임시로 전환
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = captureCam;
                        canvas.overrideSorting = true;
                        canvas.sortingOrder = 9999;  // 위에 그리기

                        // 카메라 위치/OrthographicSize 세팅
                        SetupCameraForRect(captureCam, rectWorldBounds);

                        // RenderTexture 생성
                        rt = new RenderTexture(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 24, RenderTextureFormat.ARGB32);
                        captureCam.targetTexture = rt;

                        // 렌더 & ReadPixels
                        Canvas.ForceUpdateCanvases();
                        captureCam.Render();

                        result = CopyFromRenderTexture(rt);
                        break;

                    case RenderMode.WorldSpace:
                        // (B) WorldSpace => Canvas 모드 안 바꿔도 됨. 
                        //     임시 카메라만 만들어서 해당 영역을 찍는다.
                        camObj = new GameObject("TempUICamera_World");
                        captureCam = camObj.AddComponent<Camera>();
                        captureCam.orthographic = true;
                        captureCam.cullingMask = 1 << captureLayer;
                        captureCam.clearFlags = CameraClearFlags.Color;
                        captureCam.backgroundColor = Color.clear;

                        // 카메라 위치/OrthographicSize 세팅
                        SetupCameraForRect(captureCam, rectWorldBounds);
                        
                        rt = new RenderTexture(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 24, RenderTextureFormat.ARGB32);
                        captureCam.targetTexture = rt;

                        Canvas.ForceUpdateCanvases();
                        captureCam.Render();

                        result = CopyFromRenderTexture(rt);
                        break;

                    case RenderMode.ScreenSpaceCamera:
                        camObj = new GameObject("TempUICamera_SSC");
                        captureCam = camObj.AddComponent<Camera>();
                        captureCam.orthographic = true;
                        captureCam.cullingMask = 1 << captureLayer;

                        // Canvas의 카메라 설정은 살짝 건드릴 수도 있고, 
                        // 혹은 그냥 overrideSorting만 켜서 이 카메라가 찍히게 할 수도 있음.
                        canvas.overrideSorting = true;
                        canvas.sortingOrder = 9999;  

                        SetupCameraForRect(captureCam, rectWorldBounds);

                        rt = new RenderTexture(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 24, RenderTextureFormat.ARGB32);
                        captureCam.targetTexture = rt;

                        Canvas.ForceUpdateCanvases();
                        captureCam.Render();

                        result = CopyFromRenderTexture(rt);
                        break;
                }
            }
            finally
            {
                // 5) 원상 복구
                // (a) 캔버스 모드/설정 복원
                canvas.renderMode = originalRenderMode;
                canvas.worldCamera = originalWorldCamera;
                canvas.overrideSorting = originalOverrideSorting;
                canvas.sortingOrder = originalSortingOrder;

                // (b) 레이어 복원
                RestoreLayerRecursively(originalLayers);

                // (c) 카메라 & RenderTexture 정리
                if (captureCam != null)
                {
                    captureCam.targetTexture = null;
                }
                if (rt != null)
                {
                    RenderTexture.active = null;
                    Object.Destroy(rt);
                }
                if (camObj != null)
                {
                    Object.Destroy(camObj);
                }
            }

            return result;
        }
        
        /// <summary>
        /// RectTransform을 캡처하여 PNG 파일로 저장하고 저장된 경로를 반환
        /// </summary>
        /// <param name="targetRect">캡처할 RectTransform</param>
        /// <param name="size">출력 이미지 크기</param>
        /// <param name="savePath">저장할 폴더 경로. null이면 기본 경로 사용</param>
        /// <param name="fileName">파일명 (확장자 제외). null이면 자동생성</param>
        /// <param name="captureLayerName">캡처에 사용할 레이어 이름</param>
        /// <returns>저장된 파일의 전체 경로. 실패시 null</returns>
        public static string CaptureToFile(
            this RectTransform targetRect,
            Vector2? size = null,
            string savePath = null,
            string fileName = null,
            string captureLayerName = "ForCapture")
        {
            // 1. 경로 처리
            savePath = string.IsNullOrEmpty(savePath) ? DefaultPath : savePath;

            try
            {
                // 저장 폴더가 없으면 생성
                Directory.CreateDirectory(savePath);

                // 2. 파일명 처리
                if (string.IsNullOrEmpty(fileName))
                {
                    // 자동 파일명 생성 (timestamp_número)
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    int counter = 0;
                    string autoFileName;
                    string fullPath;

                    do
                    {
                        autoFileName = counter == 0 
                            ? $"capture_{timestamp}.png"
                            : $"capture_{timestamp}_{counter}.png";
                        fullPath = Path.Combine(savePath, autoFileName);
                        counter++;
                    } while (File.Exists(fullPath));

                    fileName = autoFileName;
                }
                else if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".png";
                }

                // 3. 캡처 실행
                size ??= ScreenSize;
                Texture2D texture = CaptureRect(targetRect, size.Value, captureLayerName);
                if (texture == null)
                {
                    Debug.LogError("[CaptureToFile] 캡처에 실패했습니다.");
                    return null;
                }

                try
                {
                    // 4. PNG로 인코딩 및 저장
                    byte[] bytes = texture.EncodeToPNG();
                    string finalPath = Path.Combine(savePath, fileName);
                    File.WriteAllBytes(finalPath, bytes);

                    Debug.Log($"[CaptureToFile] 캡처 저장 완료: {finalPath}");
                    return finalPath;
                }
                finally
                {
                    // 5. 임시 텍스처 정리
                    UnityEngine.Object.Destroy(texture);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CaptureToFile] 파일 저장 중 오류 발생: {ex.Message}");
                return null;
            }
        }
    
        /// <summary>
        /// 캡처 파일을 안전하게 삭제
        /// </summary>
        /// <param name="filePath">삭제할 파일의 전체 경로</param>
        /// <returns>삭제 성공 여부 (파일이 없는 경우 false)</returns>
        public static bool DeleteCaptureFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[DeleteCaptureFile] 파일 경로가 비어있습니다.");
                return false;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[DeleteCaptureFile] 파일 삭제 완료: {filePath}");
                    return true;
                }
            
                Debug.Log($"[DeleteCaptureFile] 파일이 존재하지 않습니다: {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeleteCaptureFile] 파일 삭제 중 오류 발생: {ex.Message}");
                return false;
            }
        }
    
        /// <summary>
        /// PNG 파일을 Texture2D로 로드
        /// </summary>
        /// <param name="filePath">PNG 파일의 전체 경로</param>
        /// <returns>생성된 Texture2D. 실패시 null</returns>
        public static Texture2D LoadTextureFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[LoadTextureFromFile] 파일 경로가 비어있습니다.");
                return null;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[LoadTextureFromFile] 파일이 존재하지 않습니다: {filePath}");
                    return null;
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2); // 크기는 LoadImage에서 자동으로 조정됨
            
                if (texture.LoadImage(fileData))
                {
                    Debug.Log($"[LoadTextureFromFile] 텍스처 로드 완료: {filePath} ({texture.width}x{texture.height})");
                    return texture;
                }
                else
                {
                    Debug.LogError($"[LoadTextureFromFile] 텍스처 로드 실패: {filePath}");
                    Object.Destroy(texture);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadTextureFromFile] 파일 로드 중 오류 발생: {ex.Message}");
                return null;
            }
        }

        #region Privates
        private static string DefaultPath => Path.Combine(Application.persistentDataPath, "ScreenShots");
        
        /// <summary>
        /// RectTransform의 월드 사각영역을 Rect로 반환 (xMin,yMin, width,height)
        /// </summary>
        private static Rect GetWorldBoundsRect(RectTransform targetRect)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var c = corners[i];
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// orthographic 카메라를 rect 범위를 딱 맞게 볼 수 있도록 설정
        /// (Canvas가 XY 평면에 놓여있다고 가정; 필요하면 회전 고려해야 함)
        /// </summary>
        private static void SetupCameraForRect(Camera cam, Rect worldRect)
        {
            cam.orthographic = true;
            float w = worldRect.width;
            float h = worldRect.height;
            float centerX = worldRect.x + w * 0.5f;
            float centerY = worldRect.y + h * 0.5f;

            cam.transform.position = new Vector3(centerX, centerY, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographicSize = h * 0.5f;

            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
        }

        /// <summary>
        /// RenderTexture로부터 Texture2D 생성 (ReadPixels)
        /// </summary>
        private static Texture2D CopyFromRenderTexture(RenderTexture rt)
        {
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// target(및 자식들)의 레이어를 captureLayer로 바꾸고, 원본 레이어를 백업
        /// </summary>
        private static void SetLayerRecursively(Transform target, int captureLayer, Dictionary<Transform, int> backup)
        {
            foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
            {
                backup[t] = t.gameObject.layer;
                t.gameObject.layer = captureLayer;
            }
        }

        /// <summary>
        /// 레이어 백업 정보를 사용해 원래대로 복원
        /// </summary>
        private static void RestoreLayerRecursively(Dictionary<Transform, int> backup)
        {
            foreach (var kvp in backup)
            {
                if (kvp.Key != null)
                    kvp.Key.gameObject.layer = kvp.Value;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// fileName 형식 예시: "save_data", "save_data.bin", "test_save.extension"
    /// 기본 저장경로(Windows): C:/Users/(사용자명)/AppData/LocalLow/(회사명)/(ProductName)/SerializeData
    /// 기본 저장경로(macOS): /Users/(사용자명)/Library/Application Support/(회사명)/(ProductName)/SerializeData
    /// </summary>
    public static class Serializer
    {
        public static void SaveData<T>(string fileName, T data, string directoryPath = null, DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return;
            }

            directoryPath ??= DefaultSerializePath;

            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                byte[] bytes = SerializationUtility.SerializeValue<T>(data, format);

                string path = Path.Combine(directoryPath, fileName);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Can't write to path: {directoryPath} / {e.Message}");
            }
        }

        public static async Task SaveDataAsync<T>(string fileName, T data, string directoryPath = null, DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return;
            }

            directoryPath ??= DefaultSerializePath;

            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                byte[] bytes = await Task.Run(() => SerializationUtility.SerializeValue<T>(data, format));
                
                string path = Path.Combine(directoryPath, fileName);
                await File.WriteAllBytesAsync(path, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"Can't write to path: {directoryPath} / {e.Message}");
            }
        }

        public static T LoadData<T>(string fileName, string directoryPath = null, DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return default;
            }

            directoryPath ??= DefaultSerializePath;

            try
            {
                string path = Path.Combine(directoryPath, fileName);
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"File not found at {path}");
                    return default;
                }

                byte[] bytes = File.ReadAllBytes(path);
                
                T result = SerializationUtility.DeserializeValue<T>(bytes, format);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Can't read from path: {directoryPath} / {e.Message}");
                return default;
            }
        }

        public static async Task<T> LoadDataAsync<T>(string fileName, string directoryPath = null, DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);
            
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return default;
            }
            
            directoryPath ??= DefaultSerializePath;

            try
            {
                string path = Path.Combine(directoryPath, fileName);
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"File not found at {path}");
                    return default;
                }

                byte[] bytes = await File.ReadAllBytesAsync(path);
                
                return await Task.Run(() => SerializationUtility.DeserializeValue<T>(bytes, format));
            }
            catch (Exception e)
            {
                Debug.LogError($"Can't read from path: {directoryPath} / {e.Message}");
                return default;
            }
        }

        #region Privates
        private static string DefaultSerializePath => Path.Combine(Application.persistentDataPath, "SerializeData");

        private static string FileNameTrimming(string fileName, DataFormat format)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;
            
            string extension = Path.GetExtension(fileName);

            string newExtension = format switch
            {
                DataFormat.Binary => ".bin",
                DataFormat.JSON => ".json",
                DataFormat.Nodes => ".asset",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
            
            if (string.IsNullOrEmpty(extension))
                return fileName + newExtension;
            
            return fileName;
        }
        #endregion
    }

    public static class ResourceSerializer
    {
        public static void SaveToResources<T>(string fileName, T data, string subfolder = "", DataFormat format = DataFormat.Binary)
        {
#if UNITY_EDITOR
            fileName = FileNameTrimming(fileName, format);

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return;
            }

            try
            {
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                string directoryPath = string.IsNullOrEmpty(subfolder) ? resourcesPath : Path.Combine(resourcesPath, subfolder);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                byte[] bytes = SerializationUtility.SerializeValue(data, format);
                string path = Path.Combine(directoryPath, fileName);
                File.WriteAllBytes(path, bytes);

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving to Resources: {e.Message}");
            }
#else
            Debug.LogError("SaveToResources only works in Editor mode");
#endif
        }

        public static async Task SaveToResourcesAsync<T>(string fileName, T data, string subfolder = "", DataFormat format = DataFormat.Binary)
        {
#if UNITY_EDITOR
            fileName = FileNameTrimming(fileName, format);

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return;
            }

            try
            {
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                string directoryPath = string.IsNullOrEmpty(subfolder) ? resourcesPath : Path.Combine(resourcesPath, subfolder);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                byte[] bytes = await Task.Run(() => SerializationUtility.SerializeValue(data, format));

                string path = Path.Combine(directoryPath, fileName);
                await File.WriteAllBytesAsync(path, bytes);

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving to Resources async: {e.Message}");
            }
#else
            Debug.LogError("SaveToResourcesAsync only works in Editor mode");
            await Task.CompletedTask;
#endif
        }

        public static T LoadFromResources<T>(string fileName, string subfolder = "", DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return default;
            }

            try
            {
                string resourcePath = string.IsNullOrEmpty(subfolder)
                    ? Path.GetFileNameWithoutExtension(fileName)
                    : Path.Combine(subfolder, Path.GetFileNameWithoutExtension(fileName));

                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

                if (textAsset == null)
                {
                    Debug.LogWarning($"Resource not found: {resourcePath}");
                    return default;
                }

                T result = SerializationUtility.DeserializeValue<T>(textAsset.bytes, format);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading from Resources: {e.Message}");
                return default;
            }
        }

        public static async Task<T> LoadFromResourcesAsync<T>(string fileName, string subfolder = "", DataFormat format = DataFormat.Binary)
        {
            fileName = FileNameTrimming(fileName, format);

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("fileName is null or empty");
                return default;
            }

            try
            {
                string resourcePath = string.IsNullOrEmpty(subfolder)
                    ? Path.GetFileNameWithoutExtension(fileName)
                    : Path.Combine(subfolder, Path.GetFileNameWithoutExtension(fileName));

                Object loaded = await Resources.LoadAsync<TextAsset>(resourcePath);

                TextAsset textAsset = loaded as TextAsset;

                if (textAsset == null)
                {
                    Debug.LogWarning($"Resource not found: {resourcePath}");
                    return default;
                }

                byte[] bytesCopy = new byte[textAsset.bytes.Length];
                Buffer.BlockCopy(textAsset.bytes, 0, bytesCopy, 0, textAsset.bytes.Length); // textAsset.bytes 접근은 메인스레드에서만 가능하다네요

                return await Task.Run(() => SerializationUtility.DeserializeValue<T>(bytesCopy, format));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading from Resources: {e.Message}");
                return default;
            }
        }

        #region Privates
        private static string FileNameTrimming(string fileName, DataFormat format)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            string newExtension = format switch
            {
                DataFormat.Binary => ".bytes", // Unity에서 바이너리 파일은 .bytes
                DataFormat.JSON => ".json",
                DataFormat.Nodes => ".asset",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };

            return fileNameWithoutExtension + newExtension;
        }
        #endregion
    }

    public static class ContextMenuManager
    {
        public static void ShowContextMenu([NotNull]Canvas rootCanvas, Vector2 position, [NotNull]params ContextElement[] contextElements)
        {
            ContextMenu menu = ContextMenuPool.Get().SetRootCanvas(rootCanvas).SetElement(contextElements).SetPosition(position);
            menu.OnFinished += () => ContextMenuPool.Release(menu);
        }

        #region Privates

        private const string CONTEXT_MENU_PREFAB_PATH = "ContextMenu/ContextMenu(Blocker)";
        private static GameObject _contextMenuPrefab;

        private static GameObject ContextMenuPrefab
        {
            get
            {
                if (_contextMenuPrefab is null)
                    _contextMenuPrefab = Resources.Load<GameObject>(CONTEXT_MENU_PREFAB_PATH);
                return _contextMenuPrefab;
            }
        }

        private static Pool<ContextMenu> _contextMenuPool;

        private static Pool<ContextMenu> ContextMenuPool
        {
            get
            {
                if (_contextMenuPool is null)
                {
                    _contextMenuPool = new
                    (
                        initSize: 3,
                        maxSize: 5,
                        createFunc: () =>
                        {
                            GameObject go = GameObject.Instantiate(ContextMenuPrefab);
                            ContextMenu contextMenu = go.GetComponent<ContextMenu>();
                            go.SetActive(false);
                            return contextMenu;
                        },
                        actionOnGet: menu => menu.gameObject.SetActive(true),
                        actionOnRelease: menu =>
                        {
                            menu.Terminate();
                            menu.gameObject.SetActive(false);
                        },
                        actionOnDestroy: GameObject.Destroy,
                        isNullPredicate: menu => menu == null || menu.gameObject == null
                    );
                }
                return _contextMenuPool;
            }
        }

        #endregion
    }

    public static class RectTransformUtils
    {
        public static List<Vector2> ConvertWorldToLocalPositions([NotNull]List<Vector2> positions, [NotNull]RectTransform anchoredRect, [NotNull]Canvas rootCanvas)
        {
            if (positions == null || positions.Count == 0 || anchoredRect == null || rootCanvas == null)
            {
                Debug.LogWarning("RectTransformUtils.ConvertWorldToLocalPositions(): Check param");
                return positions;
            }
         
            List<Vector2> localPositions = new List<Vector2>(positions.Count);
   
            foreach (Vector2 worldPos in positions)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    anchoredRect, 
                    worldPos,
                    rootCanvas.renderMode == RenderMode.WorldSpace ? rootCanvas.worldCamera : null,
                    out localPoint
                );
                localPositions.Add(localPoint);
            }

            return localPositions;
        }

        public static Vector2 ConvertWorldToLocalPosition(Vector2 position, [NotNull]RectTransform anchoredRect, [NotNull]Canvas rootCanvas)
        {
            if (anchoredRect == null || rootCanvas == null)
            {
                Debug.LogWarning("RectTransformUtils.ConvertWorldToLocalPosition(): Check param.");
                return position;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                anchoredRect, 
                position,
                rootCanvas.renderMode == RenderMode.WorldSpace ? rootCanvas.worldCamera : null,
                out localPoint
            );

            return localPoint;
        }

        public static List<Vector2> ConvertLocalToWorldPositions([NotNull]List<Vector2> localPositions, [NotNull]RectTransform anchoredRect, [NotNull]Canvas rootCanvas)
        {
            if (localPositions == null || localPositions.Count == 0 || anchoredRect == null || rootCanvas == null)
            {
                Debug.LogWarning("RectTransformUtils.ConvertLocalToWorldPositions(): Check param.");
                return localPositions;
            }
    
            List<Vector2> worldPositions = new List<Vector2>(localPositions.Count);

            foreach (Vector2 localPos in localPositions)
            {
                Vector3 worldPosition = anchoredRect.TransformPoint(localPos);
                
                if (rootCanvas.renderMode == RenderMode.WorldSpace)
                    worldPositions.Add(rootCanvas.worldCamera.WorldToScreenPoint(worldPosition));
                else
                    worldPositions.Add(worldPosition);
            }

            return worldPositions;
        }

        public static Vector2 ConvertLocalToWorldPosition(Vector2 localPosition, [NotNull]RectTransform anchoredRect, [NotNull]Canvas rootCanvas)
        {
            if (anchoredRect == null || rootCanvas == null)
            {
                Debug.LogWarning("RectTransformUtils.ConvertLocalToWorldPosition(): Check param.");
                return localPosition;
            }
            
            Vector3 worldPosition = anchoredRect.TransformPoint(localPosition);
            
            if (rootCanvas.renderMode == RenderMode.WorldSpace)
                return rootCanvas.worldCamera.WorldToScreenPoint(worldPosition);
            
            return worldPosition;
            
        }

        public static void PositionRectTransformByRatio([NotNull]this RectTransform rectTransform, RectTransform parentRect, Vector2 positionRatio)
        {
            if (rectTransform && parentRect)
            {
                Vector2 parentSize = parentRect.rect.size;

                Vector2 localPosition = new Vector2
                (
                    parentSize.x * positionRatio.x - parentSize.x * 0.5f,
                    parentSize.y * positionRatio.y - parentSize.y * 0.5f
                );

                rectTransform.localPosition = localPosition;
            }
        }

        public static Canvas GetRootCanvas([NotNull]this RectTransform rectTransform) => rectTransform.GetComponentInParent<Canvas>().rootCanvas;
        public static RectTransform GetRootCanvasRect(this RectTransform rectTransform)
        {
             return rectTransform.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
        }

        public static void SetAnchor([NotNull]this RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        public static void SetOffset([NotNull]this RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        public static void SetEdges([NotNull]this RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void SetRectFull([NotNull]this RectTransform rect)
        {
            rect.SetAnchor(Vector2.zero, Vector2.one);
            rect.SetEdges(0f, 0f, 0f, 0f);
        }

        public static void SetParent([CanBeNull]Transform parent, params Transform[] childs)
        {
            foreach (Transform child in childs)
                child.SetParent(parent);
        }

        public static void SetXPos([NotNull]this RectTransform rect, float value)
        {
            Vector2 position = rect.anchoredPosition;
            position.x = value;
            rect.anchoredPosition = position;
        }

        public static void SetYPos([NotNull]this RectTransform rect, float value)
        {
            Vector2 position = rect.anchoredPosition;
            position.y = value;
            rect.anchoredPosition = position;
        }

        public static Vector2 GetNormalizeLocalPosition(RectTransform parent, RectTransform child)
        {
            if (parent == null || child == null)
            {
                throw new ArgumentNullException($"GetNormalizeLocalPosition(): Argument is null. Parent: {parent} / Child: {child}");
            }

            Vector2 parentSize = parent.rect.size;
            Vector2 childLocalPosition = child.localPosition;
            return childLocalPosition / parentSize;
        }

        public static void SetLocalPositionAsNormalizeValue(RectTransform parent, RectTransform child, Vector2 normalizeValue)
        {
            if (parent == null || child == null)
            {
                throw new ArgumentNullException($"SetLocalPositionAsNormalizeValue(): Argument is null. Parent: {parent} / Child: {child}");
            }

            if (Mathf.Abs(normalizeValue.x) > 0.5f || Mathf.Abs(normalizeValue.y) > 0.5f)
            {
                Debug.LogError($"Normalized position out of bounds: {normalizeValue}. Values must be within ±0.5 range.");
                return;
            }

            child.localPosition = parent.rect.size * normalizeValue;
        }
    }

    public static class TextGetterManager
    {
        #region Privates
        private static GameObject _textGetterPrefab;
        private static TextGetter _currentTextGetter;
        private static bool _isShow = false;

        private static string PREFAB_PATH = "StaticUI/TextGetter";

        private static GameObject TextGetterPrefab
        {
            get
            {
                _textGetterPrefab ??= Resources.Load<GameObject>(PREFAB_PATH);
                return _textGetterPrefab;
            }
        }

        private static TextGetter CurrentTextGetter
        {
            get
            {
                if (CurrentIsNull)
                {
                    _currentTextGetter = Object.Instantiate(TextGetterPrefab).GetComponent<TextGetter>();
                    _currentTextGetter.OnExit += () =>
                    {
                        _isShow = false;
                        _currentTextGetter.Rect.SetParent(null);
                    };
                }
                
                _currentTextGetter.gameObject.SetActive(false);
                return _currentTextGetter;
            }
        }

        private static bool CurrentIsNull => _currentTextGetter.IsUnityNull();

        private static void SetRect(RectTransform rect, Canvas rootCanvas)
        {
            rect.SetParent(rootCanvas.transform);
            rect.SetAnchor(Vector2.zero, Vector2.one);
            rect.SetEdges(0f, 0f, 0f, 0f);
        }
        #endregion

        public static void Set(Canvas rootCanvas, Action<string> callback, string titleString, string inputString = "", Action onExit = null)
        {
            if (_isShow && CurrentIsNull)
                _isShow = false;

            if (_isShow)
            {
                Debug.LogWarning("TextGetter is already open.");
                return;
            }

            SetRect(CurrentTextGetter.Rect, rootCanvas);
            CurrentTextGetter.Set(titleString, inputString, callback, onExit);
            _isShow = true;
        }
    }

    public static class MessageBoxManager
    {
        #region Privates
        private static GameObject _messageBoxPrefab;
        private static MessageBox _currentMessageBox;
        private static bool _isShow = false;
        private static string PREFAB_PATH = "StaticUI/MessageBox";

        private static GameObject MessageBoxPrefab
        {
            get
            {
                _messageBoxPrefab ??= Resources.Load<GameObject>(PREFAB_PATH);
                return _messageBoxPrefab;
            }
        }

        private static MessageBox CurrentMessageBox
        {
            get
            {
                if (CurrentIsNull)
                {
                    _currentMessageBox = Object.Instantiate(MessageBoxPrefab).GetComponent<MessageBox>();
                    _currentMessageBox.OnExit += () =>
                    {
                        _isShow = false;
                        _currentMessageBox.transform.SetParent(null);
                    };
                }

                _currentMessageBox.gameObject.SetActive(false);
                return _currentMessageBox;
            }
        }

        private static bool CurrentIsNull => _currentMessageBox.IsUnityNull();

        private static void SetRect(RectTransform rect, Canvas rootCanvas)
        {
            rect.SetParent(rootCanvas.transform);
            rect.SetAnchor(Vector2.zero, Vector2.one);
            rect.SetEdges(0f, 0f, 0f, 0f);
        }
        #endregion

        public static void Show(Canvas rootCanvas, string title, List<string> buttonTexts, List<Action> buttonActions)
        {
            if (_isShow && CurrentIsNull)
                _isShow = false;

            if (_isShow)
            {
                Debug.LogWarning("MessageBox is already open.");
                return;
            }

            SetRect(CurrentMessageBox.transform as RectTransform, rootCanvas);
            CurrentMessageBox.Set(title, buttonTexts, buttonActions);
            _isShow = true;
        }

        public static void ShowOk(Canvas rootCanvas, string title, Action onOk = null)
        {
            List<string> buttonTexts = new List<string> { "OK" };
            List<Action> buttonActions = new List<Action> { onOk };
            Show(rootCanvas, title, buttonTexts, buttonActions);
        }

        public static void ShowYesNo(Canvas rootCanvas, string title, Action onYes = null, Action onNo = null)
        {
            List<string> buttonTexts = new List<string> { "Yes", "No" };
            List<Action> buttonActions = new List<Action> { onYes, onNo };
            Show(rootCanvas, title, buttonTexts, buttonActions);
        }
    }

    public static class FileBrowser
    {
        public static void Save(string saveTarget, string defaultName, string[] extensions, string title = "Save", string startPath = null, Action<string> logger = null)
        {
            ExtensionFilter[] filter = extensions.Select(extension => new ExtensionFilter("", extension)).ToArray();

            if (string.IsNullOrEmpty(startPath) || !Directory.Exists(startPath))
                startPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string path = StandaloneFileBrowser.SaveFilePanel(title, startPath, defaultName, filter);

            try
            {
                File.WriteAllText(path, saveTarget);
                logger?.Invoke($"저장 완료: {path}");
            }
            catch
            {
                logger?.Invoke($"저장 실패: {path}");
            }
        }

        public static string Load(string[] extensions, string title = "Open File", string startPath = null, Action<string> logger = null)
        {
            if (extensions == null || extensions.Length == 0)
                extensions = new[] { "txt", "json", "xml" };

            ExtensionFilter[] filter = { new("Text Files", extensions) };

            if (string.IsNullOrEmpty(startPath) || !Directory.Exists(startPath))
                startPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string[] paths = StandaloneFileBrowser.OpenFilePanel(title, startPath, filter, false);

            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                return null;

            try
            {
                string content = File.ReadAllText(paths[0]);
                logger?.Invoke($"불러오기 성공: {paths[0]}");
                return content;
            }
            catch
            {
                logger?.Invoke($"불러오기 실패: {paths[0]}");
                return null;
            }
        }
    }
}