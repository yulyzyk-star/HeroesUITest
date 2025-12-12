using UnityEngine;
using UnityEngine.UI;

namespace AzurGames.UI
{
    /// <summary>
    /// Управляет выбором навыка и перемещением индикатора.
    /// SelectionIndicator должен быть дочерним элементом SkillsContainer (рядом с кнопками).
    /// </summary>
    public class SkillSelector : MonoBehaviour
    {
        [Header("Skill Buttons")]
        [Tooltip("Перетащите сюда все SkillButton в порядке слева направо")]
        [SerializeField] private Button[] _skillButtons;

        [Header("Selection Indicator")]
        [Tooltip("Перетащите сюда SelectionIndicator")]
        [SerializeField] private RectTransform _selectionIndicator;

        [Header("Settings")]
        [Tooltip("Скорость перемещения индикатора")]
        [SerializeField] private float _moveSpeed = 15f;
        
        [Tooltip("Смещение Y от центра кнопки (отрицательное = ниже)")]
        [SerializeField] private float _yOffset = -40f;

        private int _selectedIndex = 0;
        private Vector2 _targetPosition;
        private Canvas _canvas;
        private Camera _canvasCamera;
        private bool _isIndicatorMoving = false;

        private void Awake()
        {
            // Находим Canvas для правильных расчётов
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _canvasCamera = _canvas.worldCamera;
            }
        }

        private void Start()
        {
            // Подписываемся на клики кнопок
            for (int i = 0; i < _skillButtons.Length; i++)
            {
                int index = i; // Важно! Копируем для замыкания
                _skillButtons[i].onClick.AddListener(() => SelectSkill(index));
            }

            // Устанавливаем начальную позицию после кадра (чтобы Layout успел отработать)
            StartCoroutine(InitializePosition());
        }

        private System.Collections.IEnumerator InitializePosition()
        {
            yield return null; // Ждём один кадр
            if (_skillButtons.Length > 0)
            {
                UpdateIndicatorPosition(0, instant: true);
            }
        }

        private void Update()
        {
            // Плавное перемещение индикатора
            if (_selectionIndicator != null && _selectionIndicator.anchoredPosition != _targetPosition && _isIndicatorMoving)
            {
                _selectionIndicator.anchoredPosition = Vector2.Lerp(
                    _selectionIndicator.anchoredPosition,
                    _targetPosition,
                    Time.deltaTime * _moveSpeed
                );
                if (Vector2.Distance(_selectionIndicator.anchoredPosition, _targetPosition) < 0.01f)
                {
                    _selectionIndicator.anchoredPosition = _targetPosition;
                    _isIndicatorMoving = false;
                }
            }
        }

        /// <summary>
        /// Выбрать навык по индексу
        /// </summary>
        public void SelectSkill(int index)
        {
            if (index < 0 || index >= _skillButtons.Length) return;
            _selectedIndex = index;
            UpdateIndicatorPosition(index, instant: false);
        }

        private void UpdateIndicatorPosition(int index, bool instant)
        {
            if (_skillButtons == null || index >= _skillButtons.Length) return;
            
            RectTransform buttonRect = _skillButtons[index].GetComponent<RectTransform>();
            RectTransform indicatorRect = _selectionIndicator;
            RectTransform indicatorParent = indicatorRect.parent as RectTransform;
            
            if (indicatorParent == null) return;
            
            // Если индикатор и кнопка в одном родителе - просто копируем anchoredPosition
            if (indicatorParent == buttonRect.parent)
            {
                _targetPosition = new Vector2(buttonRect.anchoredPosition.x, buttonRect.anchoredPosition.y + _yOffset);
            }
            else
            {
                // Разные родители - конвертируем через screen space (учитывает anchors/pivot)
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_canvasCamera, buttonRect.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    indicatorParent,
                    screenPos,
                    _canvasCamera,
                    out Vector2 localPoint
                );
                _targetPosition = new Vector2(localPoint.x, localPoint.y + _yOffset);
            }

            if (instant)
            {
                indicatorRect.anchoredPosition = _targetPosition;
            }
            else
            {
                _isIndicatorMoving = true;
            }
        }

        /// <summary>
        /// Обновить позицию индикатора (вызывать после изменения количества кнопок)
        /// </summary>
        public void RefreshIndicator()
        {
            if (_selectedIndex < _skillButtons.Length)
            {
                UpdateIndicatorPosition(_selectedIndex, instant: true);
            }
            else if (_skillButtons.Length > 0)
            {
                SelectSkill(0);
            }
        }

        /// <summary>
        /// Установить кнопки динамически
        /// </summary>
        public void SetButtons(Button[] buttons)
        {
            // Отписываемся от старых
            foreach (var btn in _skillButtons)
            {
                btn.onClick.RemoveAllListeners();
            }
            
            _skillButtons = buttons;
            
            // Подписываемся на новые
            for (int i = 0; i < _skillButtons.Length; i++)
            {
                int index = i;
                _skillButtons[i].onClick.AddListener(() => SelectSkill(index));
            }
            
            RefreshIndicator();
        }

        /// <summary>
        /// Получить индекс выбранного навыка
        /// </summary>
        public int GetSelectedIndex()
        {
            return _selectedIndex;
        }
    }
}
