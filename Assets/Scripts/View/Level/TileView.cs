using DG.Tweening;
using UnityEngine;

namespace Helpers
{
	public class TileView : MonoBehaviour
	{
		public GameObject BreadCrumb;

		public void SetBreadCrumbVisible(bool isVisible, float delay = 0)
		{
			BreadCrumb.gameObject.SetActive(isVisible);

			if (isVisible && !DOTween.IsTweening(BreadCrumb.transform))
			{
				BreadCrumb.transform.DOJump(BreadCrumb.transform.position, 0.15f, 1, 0.175f).SetDelay(delay);
			}
		}
	}
}