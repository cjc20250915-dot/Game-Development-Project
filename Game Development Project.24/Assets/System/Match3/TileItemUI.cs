using UnityEngine;
using UnityEngine.UI;
using System;

public class TileItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    private Button button;

    public int X { get; private set; }
    public int Y { get; private set; }

    private Action<TileItemUI> onClick;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (icon == null) icon = GetComponent<Image>();
    }

    public void Init(int x, int y, Action<TileItemUI> clickCallback)
    {
        X = x;
        Y = y;
        onClick = clickCallback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void SetSprite(Sprite sprite)
    {
        if (icon != null) icon.sprite = sprite;
    }
}
