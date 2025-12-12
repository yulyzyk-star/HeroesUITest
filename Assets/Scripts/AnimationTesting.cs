using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AnimationTesting : MonoBehaviour
{
    [Header("Content")]
    public RectTransform LeftContentPanel;
    public RectTransform RightContentPanel;
    
    [Header("Hero")]
    public RectTransform Hero;
    
    [Header("Arrows")]
    public RectTransform LeftArrow;
    public RectTransform RightArrow;

    [Header("Animation Settings")]
    [SerializeField] private float _slideDuration = 0.5f;
    [SerializeField] private float _fadeDuration = 0.1f;
    [SerializeField] private float _contentOffset = 300f;
    [SerializeField] private float _heroOffset = 100f;
    [SerializeField] private float _arrowsDelay = 0.05f;
    [SerializeField] private float _arrowScaleStart = 0.7f;
    [SerializeField] private Ease _easeType = Ease.OutBack;

    private bool _isAnimating;
    private bool _initialized;
    
    // Оригинальные позиции (сохраняются один раз)
    private Vector2 _leftContentOriginalPos;
    private Vector2 _rightContentOriginalPos;
    private Vector2 _heroOriginalPos;

    IEnumerator Start()
    {
        // Ждём конец кадра, чтобы Layout Groups обновились
        yield return new WaitForEndOfFrame();
        
        // Сохраняем оригинальные позиции ПОСЛЕ полной инициализации Layout
        _leftContentOriginalPos = LeftContentPanel.anchoredPosition;
        _rightContentOriginalPos = RightContentPanel.anchoredPosition;
        _heroOriginalPos = Hero.anchoredPosition;
        
        _initialized = true;
        
        OnShowScreen();
    }

    public void OnShowScreen()
    {
        if (_isAnimating || !_initialized)
            return;

        _isAnimating = true;

        // Сбрасываем позиции к оригинальным перед подготовкой
        LeftContentPanel.anchoredPosition = _leftContentOriginalPos;
        RightContentPanel.anchoredPosition = _rightContentOriginalPos;
        Hero.anchoredPosition = _heroOriginalPos;

        // Подготовка контента и персонажа (со смещением от оригинальной позиции)
        PrepareSlideAnimation(LeftContentPanel, new Vector2(-_contentOffset, 0));   // Слева
        PrepareSlideAnimation(RightContentPanel, new Vector2(_contentOffset, 0));   // Справа
        PrepareSlideAnimation(Hero, new Vector2(_heroOffset, 0));                   // Справа (небольшое)

        // Подготовка стрелок (только fade + scale, без смещения)
        PrepareFadeAnimation(LeftArrow, _arrowScaleStart);
        PrepareFadeAnimation(RightArrow, _arrowScaleStart);

        // Создаём последовательность анимаций
        var sequence = DOTween.Sequence();

        // Контенты и персонаж выезжают одновременно
        sequence.Join(AnimateSlideIn(LeftContentPanel, _leftContentOriginalPos));
        sequence.Join(AnimateSlideIn(RightContentPanel, _rightContentOriginalPos));
        sequence.Join(AnimateSlideIn(Hero, _heroOriginalPos));

        // Стрелки появляются после контента
        sequence.Insert(_slideDuration + _arrowsDelay, AnimateFadeIn(LeftArrow));
        sequence.Insert(_slideDuration + _arrowsDelay, AnimateFadeIn(RightArrow));

        sequence.OnComplete(() => _isAnimating = false);
        sequence.Play();
    }

    private void PrepareSlideAnimation(RectTransform rect, Vector2 offset)
    {
        var canvasGroup = GetOrAddCanvasGroup(rect.gameObject);
        canvasGroup.alpha = 0f;
        rect.anchoredPosition += offset;
    }

    private void PrepareFadeAnimation(RectTransform rect, float startScale)
    {
        var canvasGroup = GetOrAddCanvasGroup(rect.gameObject);
        canvasGroup.alpha = 0f;
        rect.localScale = Vector3.one * startScale;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        var canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = obj.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    private Tween AnimateSlideIn(RectTransform rect, Vector2 targetPosition)
    {
        var canvasGroup = rect.GetComponent<CanvasGroup>();
        
        var seq = DOTween.Sequence();
        seq.Join(canvasGroup.DOFade(1f, _slideDuration));
        seq.Join(rect.DOAnchorPos(targetPosition, _slideDuration).SetEase(_easeType));

        return seq;
    }

    private Tween AnimateFadeIn(RectTransform rect)
    {
        var canvasGroup = rect.GetComponent<CanvasGroup>();
        float duration = _fadeDuration;

        var seq = DOTween.Sequence();
        seq.Join(canvasGroup.DOFade(1f, duration));
        seq.Join(rect.DOScale(Vector3.one, duration).SetEase(_easeType));

        return seq;
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
