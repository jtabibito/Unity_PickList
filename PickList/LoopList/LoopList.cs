using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace LoopList
{

    public class LoopList
    {
        public RectTransform m_rtRoot;

        int m_Row = 1; //排
        bool m_IsVertical = true;

        public Vector2 m_v2Spacing = Vector2.zero; //间距

        /// <summary>
        /// 提示向上（左）滑动的标记
        /// </summary>
        GameObject m_PointingFirstArrow;
        /// <summary>
        /// 提示向下（右）滑动的标记
        /// </summary>
        GameObject m_PointingEndArrow;


        /// <summary>
        /// 可视范围的宽度
        /// </summary>
        float m_ViewWidth;

        /// <summary>
        /// 可视范围的高度
        /// </summary>
        float m_ViewHeight;

        /// <summary>
        /// 显示内容宽度
        /// </summary>
        float m_ContentWidth;

        /// <summary>
        /// 显示内容高度
        /// </summary>
        float m_ContentHeight;  

        RectTransform m_ContentRectTrans;

        ScrollRect m_ScrollRect;

        bool m_IsShowList = false;
        int m_MaxCount = 0; //列表数量
        int m_MinIndex = -1;
        int m_MaxIndex = -1;

        float m_CellObjectWidth;
        float m_CellObjectHeight;
        GameObject m_CellGo; //指定的cell
        string m_strPrefabPath;
        float scrollMark = 0.0f;

        public RectOffset m_pPadding;

        //记录 物体的坐标 和 物体 
        class CellInfo
        {
            public Vector2 pos;
            public RectTransform rectTF;
        };
        List<CellInfo> m_CellInfos = new List<CellInfo>();
        //对象池 机制  (存入， 取出) cell
        Stack<RectTransform> poolsObj = new Stack<RectTransform>();

        public Action<RectTransform, int> CallBack;

        public void Init(RectTransform rtRoot)
        {

            m_ScrollRect = m_rtRoot.GetComponent<ScrollRect>();
            m_ContentRectTrans = m_ScrollRect.content;
            m_IsVertical = m_ScrollRect.vertical;
            Rect planeRect = m_rtRoot.rect;
            m_ViewHeight = planeRect.height;
            m_ViewWidth = planeRect.width;

            Rect contentRect = m_ContentRectTrans.rect;
            m_ContentHeight = contentRect.height;
            m_ContentWidth = contentRect.width;

            CheckAnchor(m_ContentRectTrans);

            m_ScrollRect.onValueChanged.RemoveAllListeners();
            //添加滑动事件
            m_ScrollRect.onValueChanged.AddListener(ScrollRectListener);

            if (m_PointingFirstArrow != null && m_PointingEndArrow != null)
            {
                m_ScrollRect.onValueChanged.AddListener(OnDragListener);
                OnDragListener(Vector2.zero);
            }
            m_IsShowList = false;
            scrollMark = 0.0f;

            m_pPadding = new RectOffset(0, 0, 0, 0);
        }

        public void SetPadding(int left, int right, int top, int bottom)
        {
            m_pPadding.left = left;
            m_pPadding.right = right;
            m_pPadding.top = top;
            m_pPadding.bottom = bottom;
        }
        public void SetSpacing(float x, float y)
        {
            m_v2Spacing.x = x;
            m_v2Spacing.y = y;
        }

        //检查 Anchor 是否正确
        private void CheckAnchor(RectTransform rectTrans)
        {
            rectTrans.pivot = Vector2.up;
            rectTrans.anchorMin = Vector2.up;
            rectTrans.anchorMax = Vector2.up;
        }


        private void AddCell(int i)
        {
            var rectTF = GetPoolsObj();
            rectTF.name = i.ToString();
            CallBack(rectTF, i);
            m_CellInfos[i].rectTF = rectTF;
            rectTF.anchoredPosition = m_CellInfos[i].pos;
        }

        private void ScrollRectListener(Vector2 value)
        {
            if (m_MaxCount == 0)
            {
                return;
            }
            float pos;
            if (m_IsVertical)
            {
                pos = m_ContentRectTrans.anchoredPosition.y;
                if (pos - scrollMark > 1.0f)
                {
                    ScrollUp();
                }
                else if (scrollMark - pos > 1.0f)
                {
                    ScrollDown();
                }
            }
            else
            {
                pos = m_ContentRectTrans.anchoredPosition.x;
                if (pos - scrollMark > 1.0f)
                {
                    ScrollDown();
                }
                else if (scrollMark - pos > 1.0f)
                {
                    ScrollUp();
                }
            }
            scrollMark = pos;
        }

        //滑动事件
        private void ScrollDown()
        {
            float pos = m_IsVertical ? m_CellInfos[m_MaxIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MaxIndex].pos.x + m_pPadding.left - m_pPadding.right;
            while (IsOutRange(pos))
            {
                if (m_MinIndex == 0)
                {
                    return;
                }
                else
                {
                    SetPoolsObj(m_CellInfos[m_MaxIndex].rectTF);
                    m_CellInfos[m_MaxIndex].rectTF = null;
                    m_MaxIndex -= 1;
                    m_MinIndex -= 1;
                    AddCell(m_MinIndex);
                    pos = m_IsVertical ? m_CellInfos[m_MaxIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MaxIndex].pos.x + m_pPadding.left - m_pPadding.right;
                }
            }
            pos = m_IsVertical ? m_CellInfos[m_MaxIndex].pos.y : m_CellInfos[m_MaxIndex].pos.x;
            float posMin = m_IsVertical ? m_CellInfos[m_MinIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MinIndex].pos.x + m_pPadding.left - m_pPadding.right;
            
            while (m_MinIndex > 0 && !IsOutRange(posMin) && !IsOutRange(pos))
            {
                m_MinIndex -= 1;
                AddCell(m_MinIndex);
                posMin = m_IsVertical ? m_CellInfos[m_MinIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MinIndex].pos.x + m_pPadding.left - m_pPadding.right;
            }
        }

        private void ScrollUp()
        {
            float pos = m_IsVertical ? m_CellInfos[m_MinIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MinIndex].pos.x + m_pPadding.left - m_pPadding.right;
            while (IsOutRange(pos))
            {
                if (m_MaxIndex == m_MaxCount - 1)
                {
                    return;
                }
                else
                {
                    SetPoolsObj(m_CellInfos[m_MinIndex].rectTF);
                    m_CellInfos[m_MinIndex].rectTF = null;
                    m_MaxIndex += 1;
                    m_MinIndex += 1;
                    AddCell(m_MaxIndex);
                    pos = m_IsVertical ? m_CellInfos[m_MinIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MinIndex].pos.x + m_pPadding.left - m_pPadding.right;
                }
            }

            pos = m_IsVertical ? m_CellInfos[m_MaxIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MaxIndex].pos.x + m_pPadding.left - m_pPadding.right;
            while (!IsOutRange(pos) && m_MaxIndex < m_MaxCount - 1)
            {
                m_MaxIndex += 1;
                AddCell(m_MaxIndex);
                pos = m_IsVertical ? m_CellInfos[m_MaxIndex].pos.y + m_pPadding.bottom - m_pPadding.top : m_CellInfos[m_MaxIndex].pos.x + m_pPadding.left - m_pPadding.right;
            }
        }

        //判断是否超出显示范围
        private bool IsOutRange(float pos)
        {
            var listP = m_ContentRectTrans.anchoredPosition;
            if (m_IsVertical)
            {
                return pos + listP.y > m_CellObjectHeight || pos + listP.y < -m_rtRoot.rect.height;
            }
            else
            {
                return pos + listP.x < -m_CellObjectWidth || pos + listP.x > m_rtRoot.rect.width;
            }
        }


        //取出 cell
        private RectTransform GetPoolsObj()
        {
            RectTransform cell = null;
            if (poolsObj.Count > 0)
            {
                cell = poolsObj.Pop();
            }

            if (cell == null)
            {
                cell = GameObject.Instantiate(m_CellGo).GetComponent<RectTransform>();
            }

            cell.SetParent(m_ContentRectTrans, false);
            CheckAnchor(cell);
            cell.localScale = Vector3.one;
            return cell;
        }

        //存入 cell
        private void SetPoolsObj(RectTransform cell)
        {
            if (cell != null)
            {
                poolsObj.Push(cell);
                cell.SetParent(m_rtRoot, false);
                cell.localScale = Vector3.zero;
            }
        }

        private void ClearPools()
        {
            while (poolsObj.Count > 0)
            {
                var cell = poolsObj.Pop();
                GameObject.Destroy(cell.gameObject);
            }
        }

        private void OnDragListener(Vector2 value)
        {
            if (m_PointingFirstArrow == null || m_PointingEndArrow == null)
            {
                return;
            }
            float normalizedPos = m_IsVertical ? m_ScrollRect.verticalNormalizedPosition : m_ScrollRect.horizontalNormalizedPosition;

            if (m_IsVertical)
            {
                if (m_ContentHeight - m_rtRoot.rect.height < 10)
                {
                    m_PointingFirstArrow.SetActive(false);
                    m_PointingEndArrow.SetActive(false);
                    return;
                }
            }
            else
            {
                if (m_ContentWidth - m_rtRoot.rect.width < 10)
                {
                    m_PointingFirstArrow.SetActive(false);
                    m_PointingEndArrow.SetActive(false);
                    return;
                }
            }

            if (normalizedPos >= 0.9)
            {
                m_PointingFirstArrow.SetActive(false);
                m_PointingEndArrow.SetActive(true);
            }
            else if (normalizedPos <= 0.1)
            {
                m_PointingFirstArrow.SetActive(true);
                m_PointingEndArrow.SetActive(false);
            }
            else
            {
                m_PointingFirstArrow.SetActive(true);
                m_PointingEndArrow.SetActive(true);
            }
        }

        private void InitCellList(int num)
        {
            for (int i = 0, count = m_CellInfos.Count; i < count; ++i)
            {
                SetPoolsObj(m_CellInfos[i].rectTF);
                m_CellInfos[i].rectTF = null;
            }

            if (num > m_MaxCount)
            {
                for (int i = 0, imax = num - m_MaxCount; i < imax; ++i)
                {
                    m_CellInfos.Add(new CellInfo());
                }
            }
            else
            {
                for (int i = m_CellInfos.Count - 1, imin = num; i >= imin; --i)
                {
                    m_CellInfos.RemoveAt(i);
                }
            }
        }

        public void SetPrefab(string strPath)
        {
            if (m_strPrefabPath == strPath)
                return;
        
            if (m_strPrefabPath != null)
            {
                //清理现有的Cell的prefab
                ClearPools();
                for (int i = 0, count = m_CellInfos.Count; i < count; ++i)
                {
                    var rectTF = m_CellInfos[i].rectTF;
                    if (rectTF != null)
                    {
                        GameObject.Destroy(rectTF.gameObject);
                        m_CellInfos[i].rectTF = null;
                    }
                }
            }
            m_strPrefabPath = strPath;
            var rtf = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(m_strPrefabPath)).transform as RectTransform;
            rtf.SetParent(m_ContentRectTrans);
            m_CellGo = rtf.gameObject;
            SetPoolsObj(rtf);
            m_CellObjectHeight = rtf.sizeDelta.y;
            m_CellObjectWidth = rtf.sizeDelta.x;
        }

        /// <summary>
        /// 显示列表
        /// </summary>
        /// <param name="num">列表中元素数量</param>
        /// <param name="row">行（列）数</param>
        /// <param name="row">行（列）数</param>
        public void ShowList(int num, int row = 1)
        {
            m_ScrollRect.velocity = Vector2.zero;
            m_IsShowList = true;
            m_Row = row;
            m_MinIndex = -1;
            m_MaxIndex = -1;
            m_ContentRectTrans.anchoredPosition = Vector2.zero;

            //-> 计算 Content 尺寸
            if (m_IsVertical)
            {
                float contentSize = (m_v2Spacing.y + m_CellObjectHeight) * Mathf.CeilToInt((float)num / m_Row) * Mathf.CeilToInt((float)num / m_Row) + (m_pPadding.top + m_pPadding.bottom - m_v2Spacing.y);
                m_ContentHeight = contentSize;
                m_ContentWidth = m_ContentRectTrans.sizeDelta.x;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, m_ContentHeight);
            }
            else
            {
                float contentSize = (m_v2Spacing.x + m_CellObjectWidth) * Mathf.CeilToInt((float)num / m_Row) * Mathf.CeilToInt((float)num / m_Row) + (m_pPadding.left + m_pPadding.right - m_v2Spacing.x);
                m_ContentWidth = contentSize;
                m_ContentHeight = m_ContentRectTrans.sizeDelta.y;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, m_ContentHeight);
            }

            InitCellList(num);

            //-> 1: 计算 每个Cell坐标并存储 2: 显示范围内的 Cell
            bool stop_add = false;
            for (int i = 0; i < num; ++i)
            {
                CellInfo cellInfo = m_CellInfos[i];
                float pos = 0;  //坐标( isVertical ? 记录Y : 记录X )
                float rowPos = 0; //计算每排里面的cell 坐标

                // * -> 计算每个Cell坐标
                if (m_IsVertical)
                {
                    pos = (m_CellObjectHeight + m_v2Spacing.y) * Mathf.FloorToInt(i / m_Row);
                    rowPos = (m_CellObjectWidth + m_v2Spacing.x) * (i % m_Row);
                    cellInfo.pos = new Vector2(rowPos + m_pPadding.left - m_pPadding.right, -pos + m_pPadding.bottom - m_pPadding.top);
                }
                else
                {
                    pos = (m_CellObjectWidth + m_v2Spacing.x) * Mathf.FloorToInt(i / m_Row);
                    rowPos = (m_CellObjectHeight + m_v2Spacing.y) * (i % m_Row);
                    cellInfo.pos = new Vector2(pos + m_pPadding.left - m_pPadding.right, -rowPos + m_pPadding.bottom - m_pPadding.top);
                }

                if (stop_add == false)
                {
                    //-> 记录显示范围中的 首位index 和 末尾index
                    m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index
                    m_MaxIndex = i; // 末尾index
                    AddCell(i);
                    //-> 计算是否超出范围
                    float cellPos = m_IsVertical ? cellInfo.pos.y + m_pPadding.bottom - m_pPadding.top : cellInfo.pos.x + m_pPadding.left - m_pPadding.right;
                    if (IsOutRange(cellPos))
                    {
                        stop_add = true;
                    }
                }
            }

            m_MaxCount = num;
            scrollMark = 0.0f;

            OnDragListener(Vector2.zero);

        }

        //实时刷新列表时用
        public void UpdateList()
        {
            if (m_MinIndex <= -1 || m_MaxIndex <= 0)
            {
                return;
            }
            for (int i = m_MinIndex; i <= m_MaxIndex; ++i)
            {
                CellInfo cellInfo = m_CellInfos[i];
                if (cellInfo.rectTF != null)
                {
                    CallBack(cellInfo.rectTF, i);
                }
                else
                {
                    AddCell(i);
                }
            }
        }

        //刷新某一项
        public void UpdateCell(int index)
        {
            if (m_CellInfos.Count > index && index >= 0)
            {
                CellInfo cellInfo = m_CellInfos[index];
                if (cellInfo.rectTF != null)
                {
                    float rangePos = m_IsVertical ? cellInfo.pos.y : cellInfo.pos.x;
                    if (!IsOutRange(rangePos))
                    {
                        CallBack(cellInfo.rectTF, index);
                    }
                }
            }
        }

        

        /// <summary>
        /// 指定下标滚入可视范围，下标从0开始
        /// </summary>
        /// <param name="index"></param>
        public void JumpTo(int index)
        {
            Vector2 listP = m_ContentRectTrans.anchoredPosition;
            float pos = 0.0f;
            if (m_IsVertical)
            {
                pos = (m_CellObjectHeight + m_v2Spacing.y) * Mathf.FloorToInt(index / m_Row);
                if (-pos + listP.y > m_CellObjectHeight)
                {
                    listP.y = pos;
                    m_ContentRectTrans.anchoredPosition = listP;
                }
                else if (-pos + listP.y - (m_CellObjectHeight + m_v2Spacing.y) < -m_rtRoot.rect.height)
                {
                    listP.y = pos + m_CellObjectHeight + m_v2Spacing.y - m_rtRoot.rect.height;
                    m_ContentRectTrans.anchoredPosition = listP;
                }
            }
            else
            {
                pos = (m_CellObjectWidth + m_v2Spacing.x) * Mathf.FloorToInt(index / m_Row);
                if (pos + listP.x < -m_CellObjectWidth)
                {
                    listP.x = pos;
                    m_ContentRectTrans.anchoredPosition = listP;
                }
                else if (pos + listP.x + m_CellObjectWidth + m_v2Spacing.x > m_rtRoot.rect.width)
                {
                    listP.x = pos + 2*listP.x + m_CellObjectWidth + m_v2Spacing.x - m_rtRoot.rect.width;
                    m_ContentRectTrans.anchoredPosition = listP;
                }
            }
        }

        public void SetOriginalPos()
        {
            m_ContentRectTrans.anchoredPosition = Vector2.zero;
        }

        public void ChangeList(int num, int row = 1)
        {
            m_ScrollRect.velocity = Vector2.zero;
            if (!m_IsShowList)
            {
                ShowList(num, row);
                return;
            }
            //-> 计算 Content 尺寸
            if (m_IsVertical)
            {
                float contentSize = (m_v2Spacing.y + m_CellObjectHeight) * Mathf.CeilToInt((float)num / m_Row) * Mathf.CeilToInt((float)num / m_Row) + (m_pPadding.top + m_pPadding.bottom - m_v2Spacing.y);
                m_ContentHeight = contentSize;
                m_ContentWidth = m_ContentRectTrans.sizeDelta.x;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, contentSize);
            }
            else
            {
                float contentSize = (m_v2Spacing.x + m_CellObjectWidth) * Mathf.CeilToInt((float)num / m_Row) * Mathf.CeilToInt((float)num / m_Row) + (m_pPadding.left + m_pPadding.right - m_v2Spacing.x);
                m_ContentWidth = contentSize;
                m_ContentHeight = m_ContentRectTrans.sizeDelta.x;
                m_ContentRectTrans.sizeDelta = new Vector2(contentSize, m_ContentHeight);
            }
            CellInfo cellInfo = null;

            if (num > m_MaxCount)
            {
                for (int i = m_MaxCount; i < num; ++i)
                {
                    cellInfo = new CellInfo();
                    float pos = 0;  //坐标( isVertical ? 记录Y : 记录X )
                    float rowPos = 0; //计算每排里面的cell 坐标
                                      //-> 计算是否超出范围
                    float cellPos = 0.0f;
                    // * -> 计算每个Cell坐标
                    if (m_IsVertical)
                    {
                        pos = (m_CellObjectHeight + m_v2Spacing.y) * Mathf.FloorToInt(i / m_Row);
                        rowPos = (m_CellObjectWidth + m_v2Spacing.x) * (i % m_Row);
                        cellInfo.pos = new Vector2(rowPos + m_pPadding.left - m_pPadding.right, -pos + m_pPadding.bottom - m_pPadding.top);
                        cellPos = -pos;
                    }
                    else
                    {
                        pos = (m_CellObjectWidth + m_v2Spacing.x) * Mathf.FloorToInt(i / m_Row);
                        rowPos = (m_CellObjectHeight + m_v2Spacing.y) * (i % m_Row);
                        cellInfo.pos = new Vector2(pos + m_pPadding.left - m_pPadding.right, -rowPos + m_pPadding.bottom - m_pPadding.top);
                        cellPos = pos;
                    }
                    m_CellInfos.Add(cellInfo);
                    if (!IsOutRange(cellPos))
                    {
                        if (m_MinIndex <= -1)
                        {
                            m_MinIndex = 0; //首位index
                        }
                        cellInfo = m_CellInfos[m_MinIndex];
                        cellPos = m_IsVertical ? cellInfo.pos.y + m_pPadding.bottom - m_pPadding.top : cellInfo.pos.x + m_pPadding.left - m_pPadding.right;
                        while (IsOutRange(cellPos) && m_MinIndex <= m_MaxIndex)
                        {
                            m_MinIndex += 1;
                            SetPoolsObj(cellInfo.rectTF);
                            cellInfo.rectTF = null;

                            cellInfo = m_CellInfos[m_MinIndex];
                            cellPos = m_IsVertical ? cellInfo.pos.y + m_pPadding.bottom - m_pPadding.top : cellInfo.pos.x + m_pPadding.left - m_pPadding.right;
                        }
                        m_MaxIndex += 1;
                        AddCell(m_MaxIndex);


                    }
                }
            }
            else
            {
                for (int i = m_CellInfos.Count - 1; i >= num; --i)
                {
                    cellInfo = m_CellInfos[i];
                    SetPoolsObj(cellInfo.rectTF);
                    float cellPos = m_IsVertical ? cellInfo.pos.y + m_pPadding.bottom - m_pPadding.top : cellInfo.pos.x + m_pPadding.left - m_pPadding.right;
                    if (!IsOutRange(cellPos))
                    {
                        m_MaxIndex -= 1;
                    }
                    m_CellInfos.RemoveAt(i);
                }

                int delta = m_MaxIndex - m_MinIndex;

                if (m_MaxIndex >= num)
                {
                    m_MaxIndex = num - 1;
                    m_MinIndex = m_MaxIndex - delta;
                    if (m_MinIndex < 0)
                        m_MinIndex = 0;
                    if (m_ViewHeight >= m_ContentHeight)
                        m_ContentRectTrans.anchoredPosition = Vector2.zero;
                    else
                        m_ContentRectTrans.anchoredPosition = new Vector2(0, m_ContentHeight - m_ViewHeight);
                }
            }


            m_MaxCount = num;

            UpdateList();
        }
    }
}
