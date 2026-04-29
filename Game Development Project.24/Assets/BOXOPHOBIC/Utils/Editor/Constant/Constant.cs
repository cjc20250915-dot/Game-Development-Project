//  Cristian Pop - https://boxophobic.com/

using UnityEngine;
using UnityEditor;

namespace Boxophobic.Constants
{
    public static class Constant
    {
        public static Color CategoryColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return Constant.ColorDarkGray;
                }
                else
                {
                    return Constant.ColorLightGray;
                }
            }
        }

        public static Color LineColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(0.15f, 0.15f, 0.15f, 1.0f);
                }
                else
                {
                    return new Color(0.65f, 0.65f, 0.65f, 1.0f);
                }
            }
        }

        public static Color ColorDarkGray
        {
            get
            {
                return new Color(0.2f, 0.2f, 0.2f, 1.0f);
            }
        }

        public static Color ColorLightGray
        {
            get
            {
                return new Color(0.82f, 0.82f, 0.82f, 1.0f);
            }
        }

        static GUIStyle _titleStyle;
        static GUIStyle _headerStyle;

        /// <summary>基于 EditorStyles，避免在 Inspector 原生重绘阶段使用按名称查找的 GUIStyle 导致异常。</summary>
        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _titleStyle;
            }
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _headerStyle;
            }
        }
    }
}

