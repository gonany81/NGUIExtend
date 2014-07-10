using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;


public class UIScrollViewRolling : UIWidgetContainer
{
	public class EventArgs
	{
		public int _index;
		public GameObject _item;
		public EventArgs(int index, GameObject item) { _index = index; _item = item; }
	}

	UIPanel mPanel;
	UIScrollView mScrollView;

	[HideInInspector]
	public GameObject _eventTarget;
	[HideInInspector]
	Action<EventArgs> _eventHandler;
	[HideInInspector]
	public string _eventMethod;
	public Action<EventArgs> GetEventMethod()
	{
		Action<EventArgs> handler = null;
		foreach (MonoBehaviour behaviour in _eventTarget.GetComponents<MonoBehaviour>())
		{
			if (behaviour == null)
				continue;
			foreach (MemberInfo memberInfo in behaviour.GetType().FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.FilterName, _eventMethod))
			{
				MethodInfo methodInfo = (MethodInfo)memberInfo;
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				if (parameterInfos.Length == 1 && parameterInfos[0].ParameterType == typeof(EventArgs))
				{
					handler = (Action<EventArgs>)Delegate.CreateDelegate(typeof(Action<EventArgs>), behaviour, _eventMethod);
					break;
				}
			}
		}
		return handler;
	}

	public GameObject _item;
	public int _itemLength = 0;
	public int _itemSize = 100;
	public int _headerMargin = 0;
	public int _footerMargin = 0;

	GameObject header;
	GameObject footer;
	void Awake()
	{
		_item.SetActive(false);
		_eventHandler = GetEventMethod();

		mScrollView = GetComponent<UIScrollView>();
		mPanel = mScrollView.panel;

		header = new GameObject("header");
		header.AddComponent("UIWidget");
		header.transform.parent = this.transform;
		header.transform.localPosition = Vector3.zero;
		header.transform.localScale = new Vector3(1, 1, 1);
		

		footer = new GameObject("footer");
		footer.AddComponent("UIWidget");
		footer.transform.parent = this.transform;
		footer.transform.localPosition = Vector3.zero;
		footer.transform.localScale = new Vector3(1, 1, 1);

		UIWidget hWidget = header.GetComponent<UIWidget>();
		UIWidget fWidget = footer.GetComponent<UIWidget>();

		if (mScrollView.movement == UIScrollView.Movement.Horizontal)
		{
			hWidget.width = _itemSize;
			hWidget.height = 2;
		}
		else
		{
			hWidget.width = 2;
			hWidget.height = _itemSize;
		}
		fWidget.width = hWidget.width;
		fWidget.height = hWidget.height;
	}

	void Start()
	{
		Vector3 hV3 = header.transform.localPosition;
		Vector3 fV3 = footer.transform.localPosition;
		
		if (mScrollView.movement == UIScrollView.Movement.Horizontal)
		{
			hV3.x = -_headerMargin;
			fV3.x = _itemSize * (_itemLength - 1) + _footerMargin;
		}
		else
		{
			hV3.y = -_headerMargin;
			fV3.y = _itemSize * (_itemLength - 1) + _footerMargin;
		}
		header.transform.localPosition = hV3;
		footer.transform.localPosition = fV3;

		mScrollView.UpdatePosition();
		LateUpdate();
	}

	List<GameObject> viewItemList = new List<GameObject>();
	int prevStartIdx = -1;
	int prevEndIdx = -1;
	void LateUpdate()
	{
		float totalSize = _itemSize * _itemLength;
		float viewSize = GetViewSize();
		float moveSize = totalSize - viewSize;

		float v = GetScrollValue();

		float t =  (moveSize + _headerMargin + _footerMargin);
		float hMarginOffset = _headerMargin / t;
		float fMarginOffset = _footerMargin / t;
		v += (hMarginOffset+fMarginOffset) * v;
		v -= hMarginOffset;
	
		float curPos = (int)(moveSize * v);

		int sIdx = (int)(curPos / _itemSize);
		if (sIdx < 0)
			sIdx = 0;

		int eIdx = (int)Mathf.Ceil((curPos + viewSize) / _itemSize) - 1;
		if (eIdx >= _itemLength)
			eIdx = _itemLength - 1;

		int viewItemLen = eIdx - sIdx;

		if (prevStartIdx == sIdx && prevEndIdx == eIdx)
			return;

		int sOffset = prevStartIdx - sIdx;
		int eOffset = eIdx - prevEndIdx;
		
		//remove Item
		if (sOffset < 0)
		{
			for (int i = sOffset; i < 0; i++)
			{
				if (viewItemList.Count == 0)
					break;				
				GameObject item = viewItemList[0];				
				viewItemList.Remove(item);
				RemoveItem(item);
			}
		}
		if (eOffset < 0)
		{
			for (int i = eOffset; i < 0; i++)
			{
				if (viewItemList.Count == 0)
					break;
				GameObject item = viewItemList[viewItemList.Count-1];
				viewItemList.Remove(item);
				RemoveItem(item);
			}
		}

		//Add Item
		if (sOffset > 0)
		{
			if (sOffset > viewItemLen)
			{
				sOffset = viewItemLen + 1;
				prevStartIdx = sIdx + viewItemLen + 1;
			}
			for (int i = 0; i < sOffset; i++)
			{
				int idx = prevStartIdx - (i + 1);
				GameObject item = GetItem(idx, idx * _itemSize);
				viewItemList.Insert(0, item);

				if( _eventHandler != null )
					_eventHandler(new EventArgs(idx, item));
			}
		}
		if (eOffset > 0)
		{
			if (eOffset > viewItemLen)
			{
				eOffset = viewItemLen + 1;
				prevEndIdx = eIdx - viewItemLen - 1;
			}
			for (int i = 0; i < eOffset; i++)
			{
				int idx = prevEndIdx + (i + 1);
				GameObject item = GetItem(idx, idx * _itemSize);
				viewItemList.Add(item);
				if (_eventHandler != null)
					_eventHandler(new EventArgs(idx, item));
			}
		}

		prevStartIdx = sIdx;
		prevEndIdx = eIdx;
		while(tempItems.Count > 1)
		{
			GameObject item = tempItems[0];
			tempItems.Remove(item);
			DestroyImmediate(item);			
		}
	}

	float GetViewSize()
	{
		float viewSize = 0;

		Bounds b = mScrollView.bounds;
		Vector2 bmin = b.min;
		Vector2 bmax = b.max;

		if (mScrollView.movement == UIScrollView.Movement.Horizontal && bmax.x > bmin.x)
		{
			Vector4 clip = mPanel.finalClipRegion;
			int intViewSize = Mathf.RoundToInt(clip.z);
			if ((intViewSize & 1) != 0) intViewSize -= 1;
			float halfViewSize = intViewSize * 0.5f;
			halfViewSize = Mathf.Round(halfViewSize);

			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
				halfViewSize -= mPanel.clipSoftness.x;

			viewSize = halfViewSize * 2f;
		}

		if (mScrollView.movement == UIScrollView.Movement.Vertical && bmax.y > bmin.y)
		{
			Vector4 clip = mPanel.finalClipRegion;
			int intViewSize = Mathf.RoundToInt(clip.w);
			if ((intViewSize & 1) != 0) intViewSize -= 1;
			float halfViewSize = intViewSize * 0.5f;
			halfViewSize = Mathf.Round(halfViewSize);

			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
				halfViewSize -= mPanel.clipSoftness.y;

			viewSize = halfViewSize * 2f;
		}

		return viewSize;
	}

	//UIScrollView::public virtual void UpdateScrollbars (bool recalculateBounds)
	float GetScrollValue()
	{
		if (mPanel == null) return 0;

		bool inverted = false;
		float contentSize = 0;
		float viewSize = 0;
		float contentMin = 0;
		float contentMax = 0;
		float viewMin = 0;
		float viewMax = 0;

		Bounds b = mScrollView.bounds;
		Vector2 bmin = b.min;
		Vector2 bmax = b.max;

		if (mScrollView.movement == UIScrollView.Movement.Horizontal && bmax.x > bmin.x)
		{
			inverted = false;
			Vector4 clip = mPanel.finalClipRegion;
			int intViewSize = Mathf.RoundToInt(clip.z);
			if ((intViewSize & 1) != 0) intViewSize -= 1;
			float halfViewSize = intViewSize * 0.5f;
			halfViewSize = Mathf.Round(halfViewSize);

			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
				halfViewSize -= mPanel.clipSoftness.x;

			contentSize = bmax.x - bmin.x;
			viewSize = halfViewSize * 2f;
			contentMin = bmin.x;
			contentMax = bmax.x;
			viewMin = clip.x - halfViewSize;
			viewMax = clip.x + halfViewSize;

			contentMin = viewMin - contentMin;
			contentMax = contentMax - viewMax;
		}

		if (mScrollView.movement == UIScrollView.Movement.Vertical && bmax.y > bmin.y)
		{
			inverted = true;
			Vector4 clip = mPanel.finalClipRegion;
			int intViewSize = Mathf.RoundToInt(clip.w);
			if ((intViewSize & 1) != 0) intViewSize -= 1;
			float halfViewSize = intViewSize * 0.5f;
			halfViewSize = Mathf.Round(halfViewSize);

			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
				halfViewSize -= mPanel.clipSoftness.y;

			contentSize = bmax.y - bmin.y;
			viewSize = halfViewSize * 2f;
			contentMin = bmin.y;
			contentMax = bmax.y;
			viewMin = clip.y - halfViewSize;
			viewMax = clip.y + halfViewSize;

			contentMin = viewMin - contentMin;
			contentMax = contentMax - viewMax;
		}

		float contentPadding;
		float v;

		if (viewSize < contentSize)
		{
			contentMin = Mathf.Clamp01(contentMin / contentSize);
			contentMax = Mathf.Clamp01(contentMax / contentSize);

			contentPadding = contentMin + contentMax;
			v = inverted ? ((contentPadding > 0.001f) ? 1f - contentMin / contentPadding : 0f) :
				((contentPadding > 0.001f) ? contentMin / contentPadding : 1f);
		}
		else
		{
			contentMin = Mathf.Clamp01(-contentMin / contentSize);
			contentMax = Mathf.Clamp01(-contentMax / contentSize);

			contentPadding = contentMin + contentMax;
			v = inverted ? ((contentPadding > 0.001f) ? 1f - contentMin / contentPadding : 0f) :
				((contentPadding > 0.001f) ? contentMin / contentPadding : 1f);

			if (contentSize > 0)
			{
				contentMin = Mathf.Clamp01(contentMin / contentSize);
				contentMax = Mathf.Clamp01(contentMax / contentSize);
				contentPadding = contentMin + contentMax;
			}
		}

		return v;
	}

	List<GameObject> tempItems = new List<GameObject>();
	GameObject GetItem(int idx, float pos)
	{
		GameObject item;
		if (tempItems.Count == 0)
		{
			UnityEngine.Object obj = GameObject.Instantiate(_item);
			item = (GameObject)obj;
			item.SetActive(true);
			item.transform.parent = this.transform;
			item.transform.localScale = new Vector3(1, 1, 1);
		}
		else
		{
			item = tempItems[0];
			tempItems.Remove(item);
		}
		item.name = "item_" + idx;

		if(mScrollView.movement == UIScrollView.Movement.Horizontal)
			item.transform.localPosition = new Vector3(idx * _itemSize, 0, 0);
		else
			item.transform.localPosition = new Vector3(0, (_itemLength - idx - 1) * _itemSize, 0);
		return item;
	}

	void RemoveItem(GameObject item)
	{
		item.name = "temp_" + tempItems.Count;
		tempItems.Add(item);
	}
}
