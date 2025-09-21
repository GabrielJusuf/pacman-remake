using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    private List<Tween> activeTweens;

    void Awake()
    {
        activeTweens = new List<Tween>();
    }

    void Update()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = activeTweens[i];
            
            if (tween.Target == null)
            {
                activeTweens.RemoveAt(i);
                continue;
            }

            float elapsed = Time.time - tween.StartTime;
            float progress = elapsed / tween.Duration;

            if (progress >= 1.0f)
            {
                tween.Target.position = tween.EndPos;
                activeTweens.RemoveAt(i);
            }
            else
            {
                tween.Target.position = Vector3.Lerp(tween.StartPos, tween.EndPos, progress);
            }
        }
    }

    public void AddTween(Transform target, Vector3 startPos, Vector3 endPos, float duration)
    {
        RemoveTween(target);
        
        Tween newTween = new Tween(target, startPos, endPos, Time.time, duration);
        activeTweens.Add(newTween);
    }

    public void RemoveTween(Transform target)
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            if (activeTweens[i].Target == target)
            {
                activeTweens.RemoveAt(i);
            }
        }
    }

    public bool IsTweening(Transform target)
    {
        foreach (Tween tween in activeTweens)
        {
            if (tween.Target == target)
                return true;
        }
        return false;
    }
}
