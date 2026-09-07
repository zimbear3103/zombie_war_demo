using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utility
{
    public class Tweener : MonoBehaviour
    {
        #region Easing
        public enum Ease
        {
            Linear,
            InQuad,
            OutQuad,
            OutBack
        }

        // Maps linear t (0..1) to the eased value. OutBack overshoots past 1 then
        // settles — pair it with LerpUnclamped for bounce/pop animations.
        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InQuad:
                    return t * t;
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float p = t - 1f;
                    return 1f + c3 * p * p * p + c1 * p * p;
                }
                default:
                    return t;
            }
        }
        #endregion

        #region IENumerator
        public static IEnumerator IE_LocalTranslate(Transform obj, Vector3 start, Vector3 end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localPosition = Vector3.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.localPosition = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_GlobalTranslate(Transform obj, Vector3 start, Vector3 end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.position = Vector3.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.position = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_LocalRotate(Transform obj, Vector3 start, Vector3 end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localEulerAngles = Vector3.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.localEulerAngles = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_GlobalRotate(Transform obj, Vector3 start, Vector3 end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.eulerAngles = Vector3.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.eulerAngles = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_LocalRotate(Transform obj, Quaternion start, Quaternion end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localRotation = Quaternion.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.localRotation = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_GlobalRotate(Transform obj, Quaternion start, Quaternion end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.rotation = Quaternion.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.rotation = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_LocalScale(Transform obj, Vector3 start, Vector3 end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localScale = Vector3.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            obj.localScale = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_TransparencyImage(Image image, float start, float end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (image == null)
                    yield break;

                image.color = Color.Lerp(new Color(image.color.r, image.color.g, image.color.b, start),
                                         new Color(image.color.r, image.color.g, image.color.b, end),
                                         t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            image.color = new Color(image.color.r, image.color.g, image.color.b, end);
            callbacks?.Invoke();
        }
        public static IEnumerator IE_DelayForAction(float delay, System.Action callback)
        {
            float t = 0;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }

            callback?.Invoke();
        }
        public static IEnumerator IE_DelayForAction(System.Func<bool> condition, System.Action callback)
        {
            yield return new WaitUntil(condition);

            callback?.Invoke();
        }
        #endregion

        #region IENumerator (eased)
        // Eased variants for effect animations (pop/bounce). LerpUnclamped lets
        // OutBack overshoot past the target before settling.
        public static IEnumerator IE_LocalScale(Transform obj, Vector3 start, Vector3 end, float duration, Ease ease, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localScale = Vector3.LerpUnclamped(start, end, Evaluate(ease, t / duration));
                t += Time.deltaTime;
                yield return null;
            }
            obj.localScale = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_LocalRotate(Transform obj, Quaternion start, Quaternion end, float duration, Ease ease, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (obj == null)
                    yield break;

                obj.localRotation = Quaternion.LerpUnclamped(start, end, Evaluate(ease, t / duration));
                t += Time.deltaTime;
                yield return null;
            }
            obj.localRotation = end;
            callbacks?.Invoke();
        }
        public static IEnumerator IE_TransparencyText(TMP_Text text, float start, float end, float duration, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                if (text == null)
                    yield break;

                text.alpha = Mathf.Lerp(start, end, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            text.alpha = end;
            callbacks?.Invoke();
        }
        // Generic workhorse: drives any value from start to end, the caller applies
        // it in onUpdate (e.g. combined scale + color in one tween).
        public static IEnumerator IE_Value(float start, float end, float duration, Ease ease, System.Action<float> onUpdate, System.Action callbacks = null)
        {
            float t = 0;
            while (t < duration)
            {
                onUpdate?.Invoke(Mathf.LerpUnclamped(start, end, Evaluate(ease, t / duration)));
                t += Time.deltaTime;
                yield return null;
            }
            onUpdate?.Invoke(end);
            callbacks?.Invoke();
        }
        #endregion
    }
}
