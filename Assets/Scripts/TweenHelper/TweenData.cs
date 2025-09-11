using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using EditorAttributes;

[System.Serializable]
public class TweenData
{
    #region enum structers
    public enum TweenType { Move, Rotate, Scale, Color, Fade, Jump }
    public enum VisualType { Image, Sprite, TMP }
    public enum MoveType { Absolute, Offset, Target }
    #endregion
    [SerializeField] bool IsTransformAutoSet;

    [HideField(nameof(IsTransformAutoSet))]
    public Transform objectToTween;
    public TweenType tweenType;
    [ShowField("@this.tweenType == TweenType.Move")]
    public MoveType moveType;
    [ShowField("@(this.tweenType == TweenType.Move || this.tweenType == TweenType.Rotate || this.tweenType == TweenType.Scale || this.tweenType == TweenType.Jump) && moveType != MoveType.Target")]
    public Vector3 targetVector;
    [ShowField("@(this.tweenType == TweenType.Move || this.tweenType == TweenType.Rotate || this.tweenType == TweenType.Scale || this.tweenType == TweenType.Jump) && moveType == MoveType.Target")]
    public RectTransform targetTransform;
    [ShowField("tweenType", TweenType.Jump)]
    public Vector2 jumpPowerRange;
    [ShowField("@this.tweenType == TweenType.Color || this.tweenType == TweenType.Fade")]
    public VisualType targetVisual;

    [ShowField("tweenType", TweenType.Color)]
    public Color targetColor;
    [ShowField("@this.tweenType == TweenType.Fade")]
    public float alpha;
    [SerializeField] bool useCurve;
    [ShowField("@this.useCurve == false")]
    public Ease ease;
    [ShowField("@this.useCurve == true")]
    [SerializeField] AnimationCurve curve;
    public float duration = 1f;
    public bool join = false;
    public bool insert = false;
    [ShowField("insert")]
    public float insertPoint;
    public bool callback;
    [ShowField("callback")]
    public UnityEvent unityEvent;

    public Tween GetTween(Transform transform = null)
    {
        if (transform != null) objectToTween = transform;
        Tween tween = null;
        switch (tweenType)
        {
            case TweenType.Move:
                RectTransform rectTransform = objectToTween as RectTransform;
                tween = DOMove();
                break;
            case TweenType.Rotate:
                tween = objectToTween.DORotate(targetVector, duration, RotateMode.FastBeyond360);
                break;
            case TweenType.Scale:
                tween = objectToTween.DOScale(targetVector, duration);
                break;
            case TweenType.Color:
                tween = DOColor();
                break;
            case TweenType.Fade:
                tween = DOFade();
                break;
            case TweenType.Jump:
                tween = DOJump();
                break;

        }
        tween = useCurve ? tween.SetEase(curve) : tween.SetEase(ease);
        if (callback && unityEvent != null)
        {
            tween.OnStepComplete(() => unityEvent.Invoke());
        }

        return tween;
    }

    private Tween DOMove()
    {
        RectTransform rectTransform = objectToTween as RectTransform;
        switch (moveType)
        {
            case MoveType.Absolute:
                return rectTransform.DOAnchorPos(targetVector, duration);
            case MoveType.Offset:
                return rectTransform.DOAnchorPos(rectTransform.anchoredPosition + new Vector2(targetVector.x, targetVector.y), duration);
            case MoveType.Target:
                return rectTransform.DOMove(targetTransform.position, duration);
        }
        return null;
    }

    private Tween DOJump()
    {
        RectTransform rectTransform = objectToTween as RectTransform;
        var jumpPower = Random.Range(jumpPowerRange.x, jumpPowerRange.y);
        return rectTransform.DOJumpAnchorPos(targetVector, jumpPower, 1, duration);
    }


    private Tween DOColor()
    {
        switch (targetVisual)
        {
            case VisualType.Image:
                return objectToTween.GetComponent<Image>().DOColor(targetColor, duration);
            case VisualType.Sprite:
                return objectToTween.GetComponent<SpriteRenderer>().DOColor(targetColor, duration);
            case VisualType.TMP:
                return objectToTween.GetComponent<TMPro.TMP_Text>().DOColor(targetColor, duration);
        }
        return null;
    }


    private Tween DOFade()
    {
        switch (targetVisual)
        {
            case VisualType.Image:
                return objectToTween.GetComponent<Image>().DOFade(alpha, duration);
            case VisualType.Sprite:
                return objectToTween.GetComponent<SpriteRenderer>().DOFade(alpha, duration);
            case VisualType.TMP:
                return objectToTween.GetComponent<TMPro.TMP_Text>().DOFade(alpha, duration);

        }
        return null;
    }


    public void AutoSelectTargetVisual()
    {
        bool requiresTargetVisual = tweenType == TweenType.Color || tweenType == TweenType.Fade;
        if (!requiresTargetVisual)
            return;

        if (!objectToTween)
            return;

        bool hasText = objectToTween.GetComponent<TMP_Text>() != null;
        bool hasImage = objectToTween.GetComponent<Image>() != null;
        bool hasSprite = objectToTween.GetComponent<SpriteRenderer>() != null;

        bool tweenComponentXOR = hasText ^ hasImage ^ hasSprite;
        if (tweenComponentXOR)
            return;

        if (hasText)
        {
            targetVisual = VisualType.TMP;
        }
        else if (hasImage)
        {
            targetVisual = VisualType.Image;
        }
        else if (hasSprite)
        {
            targetVisual = VisualType.Sprite;
        }
    }
}

[System.Serializable]
public class TweenDataList
{
    [SerializeField] string name;
    public List<TweenData> TweenDatas;

    public DG.Tweening.Sequence BuildSequence(Transform transform = null)
    {
        DG.Tweening.Sequence newSequence = DOTween.Sequence();
        newSequence.Pause();
        newSequence.SetAutoKill(false);
        foreach (var tweenData in TweenDatas)
        {
            if (tweenData.join)
            {
                newSequence.Join(tweenData.GetTween(transform));
            }
            else if (tweenData.insert)
            {
                newSequence.Insert(tweenData.insertPoint, tweenData.GetTween(transform));
            }
            else
            {
                newSequence.Append(tweenData.GetTween(transform));
            }
        }

        return newSequence;
    }
}
