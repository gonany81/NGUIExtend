using UnityEngine;
using System.Collections;

public class ScrollViewMain : MonoBehaviour 
{
	public void UpdateItem(UIScrollViewRolling.EventArgs e)
	{
		Debug.LogWarning("index=" + e._index + ", itemName=" + e._item.name);

		UISprite sprite = e._item.GetComponent<UISprite>();
		Random.seed = e._index;
		int type = Random.Range(1, 6);
		sprite.spriteName = "image_0" + type;

		UILabel label = e._item.GetComponentInChildren<UILabel>();
		label.text = e._index.ToString();
	}
}
