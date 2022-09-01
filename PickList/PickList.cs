using System;
using System.Collections.Generic;
using UnityEngine;
using CLoopList = LoopList.LoopList;

namespace PickList
{
    public abstract class PickData
    {
        private int m_iIndex = 0;
        public int Index { get => m_iIndex; set => m_iIndex = value; } // 索引会在PickList设置数据集时赋值，所以不需要在外部设置
        private bool m_bPicked = false;
        public bool Picked { get => m_bPicked; set => m_bPicked = value; } // 数据被选中参数，如果需要初始设置默认选中，这个参数应该设置为true
        public virtual bool IsDirty() { return false; } // 脏数据判断
    }
    
    public class PickItem
    {
        public RectTransform m_rtRoot;

        public PickData m_pData;
        public int GetIndex() => m_pData.Index;

        private Action<PickData> _m_pOnPick;

        public void Init(RectTransform rtRoot)
        {
            m_rtRoot = rtRoot;
        }

        public void AddPickListener(Action<PickData> pOnPick)
        {
            _m_pOnPick += pOnPick;
        }
        public void RemovePickListener(Action<PickData> pOnPick)
        {
            _m_pOnPick -= pOnPick;
        }
        public void RemoveAllListeners()
        {
            _m_pOnPick = null;
        }
        public void OnPick()
        {
            if (_m_pOnPick != null)
            {
                _m_pOnPick(this.m_pData);
            }
        }

        public virtual void Refresh() { } // 刷新数据UI逻辑
        public virtual void SetPick() { } // 设置选中UI显示逻辑
        public virtual void SetUnPick() { } // 设置取消选中UI显示逻辑
    }

    /**
     * 选择列表
     * PickItem: 选择项,需要自己实现内部功能
     * PickData: 选择数据，需要自己实现内部数据
     * 调用Init进行初始化，根节点指向ScrollRect
     */
    public class PickList<T> where T : PickItem, new()
    {
        public RectTransform m_rtRoot;

        protected bool _m_bSingleUnPickable;
        protected bool _m_bMultiPickable; // 支持多选
        protected bool _m_bDisablePick; // 禁止选择

        protected Comparison<PickData> _m_pComparison;
        protected List<PickData> _m_listPickItemDs; // 选择列表

        protected int _m_iCurPicked = -1;
        protected Dictionary<int, PickData> _m_dictPickItems; // 存储选择的PickItem

        /// <summary>
        /// 获取数据集大小
        /// </summary>
        public int Count { get => _m_listPickItemDs.Count; }
        /// <summary>
        /// 获取已选数据数量
        /// </summary>
        public int PickedCount { get => _m_dictPickItems.Count; }
        /// <summary>
        /// 获取当前选择项
        /// Note: 列表不知道PickData实际类型，所以没有选中时，这个值为null
        /// </summary>
        public PickData Current {
            get
            {
                if (_m_iCurPicked >= 0)
                {
                    return _m_dictPickItems[_m_iCurPicked];
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// 选择上一项
        /// </summary>
        public void Prev()
        {
            if (_m_listPickItemDs.Count == 0 || _m_listPickItemDs.Count == 0)
                return;
            OnPick(_m_listPickItemDs[(_m_iCurPicked-1 + _m_listPickItemDs.Count) % _m_listPickItemDs.Count]);
            JumpTo(_m_listPickItemDs[_m_iCurPicked]);
        }
        /// <summary>
        /// 选择下一项
        /// </summary>
        public void Next()
        {
            if (_m_listPickItemDs.Count == 0 || _m_listPickItemDs.Count == 0)
                return;
            OnPick(_m_listPickItemDs[(_m_iCurPicked+1) % _m_listPickItemDs.Count]);
            JumpTo(_m_listPickItemDs[_m_iCurPicked]);
        }

        protected Func<PickData, bool, bool> _m_pfnOnPick;
        protected Func<PickData, bool, bool> _m_pfnOnUnPick;

        protected int _m_iConstraint;
        protected CLoopList _m_pLoopList;
        protected Dictionary<RectTransform, T> _m_dictLoopItems; // 存储用于循环的PickItem

        public PickList()
        {
            _m_bSingleUnPickable = true;
            _m_bMultiPickable = false;
            _m_bDisablePick = false;

            _m_iCurPicked = -1;
            _m_dictPickItems = new Dictionary<int, PickData>(8);

            _m_pLoopList = new CLoopList();
            _m_pLoopList.CallBack = RefreshPickItem;
            _m_dictLoopItems = new Dictionary<RectTransform, T>(8);
        }

        ~PickList()
        {
            _m_pComparison = null;
            _m_listPickItemDs.Clear();
            _m_listPickItemDs = null;

            _m_dictPickItems.Clear();
            _m_dictPickItems = null;

            _m_pfnOnPick = null;
            _m_pfnOnUnPick = null;

            _m_pLoopList = null;
            _m_dictLoopItems.Clear();
            _m_dictLoopItems = null;
        }

        public void Init(RectTransform rtRoot)
        {
            m_rtRoot = rtRoot;
            _m_pLoopList.Init(rtRoot);
        }

        /// <summary>
        /// 设置循环列表加载的预制名
        /// </summary>
        public void SetPrefab(string strPath)
        {
            _m_pLoopList.SetPrefab(strPath);
        }
        /// <summary>
        /// 设置循环列表填充方式
        /// </summary>
        public void SetPadding(int iLeft, int iRight, int iTop, int iBottom)
        {
            _m_pLoopList.SetPadding(iLeft, iRight, iTop, iBottom);
        }
        /// <summary>
        /// 设置循环列表间距
        /// </summary>
        public void SetSpacing(float x, float y)
        {
            _m_pLoopList.SetSpacing(x, y);
        }
        /// <summary>
        /// 设置循环列表行或列常数
        /// </summary>
        public void SetConstraint(int iConstraint)
        {
            _m_iConstraint = iConstraint;
        }
        
        /// <summary>
        /// 设置选择属性
        /// </summary>
        /// <param name="SingleUnPickable">SingleUnPickable:bool,是否支持单选取消</param>
        /// <param name="MultiPickable">SingleUnPickable:bool,是否支持多选</param>
        public void SetPickProperty(Dictionary<string, object> argv = null)
        {
            if (argv != null)
            {
                object value;
                if (argv.TryGetValue("SingleUnPickable", out value))
                {
                    _m_bSingleUnPickable = (bool)value;
                }
                if (argv.TryGetValue("MultiPickable", out value))
                {
                    _m_bMultiPickable = (bool)value;
                }
            }
        }
        /// <summary>
        /// 设置禁止选择
        /// </summary>
        public void SetDisablePick(bool bDisablePick)
        {
            _m_bDisablePick = bDisablePick;
        }
        /// <summary>
        /// 设置排序方式
        /// </summary>
        public void SetComparison(Comparison<PickData> pComparison)
        {
            _m_pComparison = pComparison;
        }
        /// <summary>
        /// 设置选择列表数据集
        /// Note: 没有KeepPick参数时，即使初始传入的数据已经设置了Picked选项，选择列表中也不会有数据
        /// </summary>
        /// <param name="argv">Argv:提供可选参数:Sort:bool,Refresh:bool,KeepPick:bool</param>
        public void SetDataSet(List<PickData> listPickItems, Dictionary<string, object> argv = null)
        {
            if (listPickItems == null)
                return;
            _m_listPickItemDs = listPickItems;
            bool bKeepPick = false;
            if (argv != null)
            {
                object value;
                if (argv.TryGetValue("Sort", out value))
                {
                    if ((bool)value && _m_pComparison != null)
                    {
                        _m_listPickItemDs.Sort(_m_pComparison);
                    }
                }
                for (int i = 0; i < _m_listPickItemDs.Count; i++)
                {
                    _m_listPickItemDs[i].Index = i;
                }
                if (argv.TryGetValue("Refresh", out value))
                {
                    if ((bool)value)
                    {
                        RefreshData();
                    }
                }
                if (argv.TryGetValue("KeepPick", out value))
                {
                    bKeepPick = (bool)value;
                }
            }
            else
            {
                for (int i = 0; i < _m_listPickItemDs.Count; i++)
                {
                    _m_listPickItemDs[i].Index = i;
                }
            }
            if (!bKeepPick)
            {
                _m_iCurPicked = -1;
                _m_dictPickItems.Clear();
            }
        }
        /// <summary>
        /// 设置选择回调, 回调参数：PickData, 是否选择相同的PickData
        /// 返回值：是否可以选择
        /// Note: 这个回调中理论上不应该处理任何显示逻辑，只是处理选择逻辑，并且选择和取消逻辑应该分开处理
        /// </summary>
        public void SetOnPick(Func<PickData, bool, bool> pfnOnPick)
        {
            _m_pfnOnPick = pfnOnPick;
        }
        /// <summary>
        /// 设置取消回调, 回调参数：PickData, 是否选择相同的PickData
        /// 返回值：是否可以取消
        /// Note: 这个回调中理论上不应该处理任何显示逻辑，只是处理选择逻辑，并且选择和取消逻辑应该分开处理
        /// </summary>
        public void SetOnUnPick(Func<PickData, bool, bool> pfnOnUnPick)
        {
            _m_pfnOnUnPick = pfnOnUnPick;
        }
        /// <summary>
        /// 获取指定索引的PickData
        /// </summary>
        public PickData GetPickData(int index)
        {
            if (index < 0 || index >= _m_listPickItemDs.Count)
                return null;
            return _m_listPickItemDs[index];
        }
        /// <summary>
        /// 获取已选列表
        /// </summary>
        public List<PickData> GetPickedList()
        {
            List<PickData> listPicked = new List<PickData>(_m_dictPickItems.Count);
            foreach (var item in _m_dictPickItems)
            {
                listPicked.Add(item.Value);
            }
            return listPicked;
        }
        /// <summary>
        /// 设置所有选择项为选中
        /// </summary>
        public void PickAll()
        {
            if (_m_bMultiPickable)
            {
                foreach (var pPickData in _m_listPickItemDs)
                {
                    pPickData.Picked = true;
                    _m_dictPickItems[pPickData.Index] = pPickData;
                }
                foreach (var pItem in _m_dictLoopItems.Values)
                {
                    pItem.SetPick();
                }
            }
        }
        /// <summary>
        /// 设置所有选择项为取消选中
        /// </summary>
        public void UnPickAll()
        {
            foreach (var pPickData in _m_dictPickItems.Values)
            {
                pPickData.Picked = false;
            }
            _m_iCurPicked = -1;
            _m_dictPickItems.Clear();
            foreach (var pItem in _m_dictLoopItems.Values)
            {
                pItem.SetUnPick();
            }
        }
        /// <summary>
        /// 刷新数据
        /// </summary>
        public void RefreshData()
        {
            if (_m_listPickItemDs == null)
                return;
            UpdateData();
            _m_pLoopList.ShowList(_m_listPickItemDs.Count, _m_iConstraint);
        }
        /// <summary>
        /// 刷新选择项显示
        /// </summary>
        public void RefreshDisplay()
        {
            foreach (var pItem in _m_dictLoopItems.Values)
            {
                pItem.Refresh();
            }
        }
        /// <summary>
        /// 更新数据，对原数据有外部Pick操作时，调用这个方法把数据更新到列表
        /// </summary>
        public void UpdateData()
        {
            if (_m_listPickItemDs == null)
                return;
            if (_m_bMultiPickable)
            {
                foreach (var pData in _m_listPickItemDs)
                {
                    if (pData.Picked)
                    {
                        _m_iCurPicked = pData.Index;
                        _m_dictPickItems[pData.Index] = pData;
                    }
                    else
                    {
                        if (_m_dictPickItems.ContainsKey(pData.Index))
                        {
                            _m_dictPickItems.Remove(pData.Index);
                        }
                    }
                }
            }
            else
            {
                int firstPick = _m_listPickItemDs.FindIndex(pData => pData.Picked);
                if (firstPick != -1)
                {
                    _m_iCurPicked = _m_listPickItemDs[firstPick].Index;
                    _m_dictPickItems[_m_listPickItemDs[firstPick].Index] = _m_listPickItemDs[firstPick];
                    for (; firstPick < _m_listPickItemDs.Count; ++firstPick)
                    {
                        _m_listPickItemDs[firstPick].Picked = false;
                        if (_m_dictPickItems.ContainsKey(_m_listPickItemDs[firstPick].Index))
                        {
                            _m_dictPickItems.Remove(_m_listPickItemDs[firstPick].Index);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 跳转至指定选择项行列
        /// </summary>
        public void JumpTo(PickData pData)
        {
            _m_pLoopList.JumpTo(pData.Index);
        }
        /// <summary>
        /// 刷新滚动
        /// </summary>
        protected void RefreshPickItem(RectTransform rtItem, int index)
        {
            if (!_m_dictLoopItems.TryGetValue(rtItem, out T pPickItem))
            {
                pPickItem = new T();
                pPickItem.Init(rtItem);
            }
            _m_dictLoopItems[rtItem] = pPickItem;
            
            pPickItem.m_pData = _m_listPickItemDs[index];
            pPickItem.RemoveAllListeners();
            pPickItem.AddPickListener(OnPick);
            pPickItem.Refresh();
        }

        /// <summary>
        /// Pick数据逻辑
        /// </summary>
        protected void Pick(PickData pData)
        {
            pData.Picked = true;
            _m_dictPickItems[pData.Index] = pData;
            foreach (var pPickItem in _m_dictLoopItems.Values)
            {
                if (pPickItem.m_pData.Index == pData.Index)
                {
                    pPickItem.SetPick();
                }
            }
        }
        protected void UnPick(PickData pData)
        {
            pData.Picked = false;
            if (_m_dictPickItems.ContainsKey(pData.Index))
            {
                _m_dictPickItems.Remove(pData.Index);
            }
            foreach (var pPickItem in _m_dictLoopItems.Values)
            {
                if (pPickItem.GetIndex() == pData.Index)
                {
                    pPickItem.SetUnPick();
                }
            }
        }
        /// <summary>
        /// 触发Pick逻辑
        /// </summary>
        public void OnPick(PickData pData)
        {
            if (this._m_bDisablePick || pData.IsDirty())
            {
                return;
            }

            bool result = true;
            if (_m_bMultiPickable)
            {
                if (_m_dictPickItems.ContainsKey(pData.Index))
                {
                    if (_m_pfnOnUnPick != null)
                    {
                        result = _m_pfnOnUnPick(pData, true);
                    }
                    
                    if (result)
                    {
                        UnPick(pData);

                        if (_m_iCurPicked == pData.Index)
                        {
                            _m_iCurPicked = -1;
                        }
                    }
                    return;
                }

                result = true;
                if (_m_pfnOnPick != null)
                {
                    result = _m_pfnOnPick(pData, false);
                }

                if (result)
                {
                    _m_iCurPicked = pData.Index;
                    Pick(pData);
                }
            }
            else
            {
                if (_m_iCurPicked == pData.Index)
                {
                    if (_m_bSingleUnPickable)
                    {
                        if (_m_pfnOnUnPick != null)
                        {
                            result = _m_pfnOnUnPick(pData, true);
                        }

                        if (result)
                        {
                            UnPick(pData);

                            if (_m_iCurPicked == pData.Index)
                            {
                                _m_iCurPicked = -1;
                                return;
                            }
                        }
                    }
                    else
                    {
                        // 如果不支持单选取消, 则执行重复选中逻辑
                        if (_m_pfnOnPick != null)
                        {
                            result = _m_pfnOnPick(pData, true);
                        }

                        _m_iCurPicked = pData.Index;
                        Pick(pData);
                    }
                }
                else
                {
                    PickData pCurPick = null;
                    if (_m_iCurPicked != -1)
                    {
                        pCurPick = _m_listPickItemDs[_m_iCurPicked];
                    }
                    bool isSame = pData == pCurPick;

                    result = true;
                    if (_m_pfnOnPick != null)
                    {
                        result = _m_pfnOnPick(pData, isSame);
                    }

                    if (result)
                    {
                        if (pCurPick != null)
                        {
                            // result = true;
                            if (_m_pfnOnUnPick != null)
                            {
                                result = _m_pfnOnUnPick(pCurPick, isSame);
                            }

                            // if (result)
                            // {
                            UnPick(pCurPick);
                            // }
                        }

                        _m_iCurPicked = pData.Index;
                        Pick(pData);
                    }
                }
            }
        }
    }
}
