using CompassNavigatorPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;

namespace LWS.SportsPresentation {
// CNP owns POI registration, projection, visibility and indicator instances. This adapter
// gives its offscreen indicator the fixed Keepaway location and unscaled presentation.
[DefaultExecutionOrder(600)]
public sealed class KeepawayBallCompass : CompassPro {
 public CompassProPOI ballPOI;
 CanvasGroup fade; Tween fading; bool shown,angleReady; float bearing,angularVelocity;
 public bool BallVisible => shown;
 public float Bearing => bearing;
 public RectTransform Widget => ballPOI == null ? null : ballPOI.GetState(compassGroup).indicatorRT;
 public float Opacity => fade == null ? 0 : fade.alpha;

 protected override void ComputePOIViewportPos(CompassProPOI poi) {
  base.ComputePOIViewportPos(poi);
  // WorldToViewportPoint is already local to Camera.rect. CNP's overlay remap
  // otherwise applies the split rectangle a second time (full-screen is unaffected).
  poi.GetState(compassGroup).viewportPos = cameraMain.WorldToViewportPoint(poi.positionWS);
 }
 protected override GameObject CreateIndicator(CompassProPOI poi) {
  var widget = base.CreateIndicator(poi);
  var wrapper = new GameObject("Keepaway ball indicator fade", typeof(RectTransform), typeof(CanvasGroup));
  wrapper.transform.SetParent(widget.transform.parent, false);
  var rect = (RectTransform)wrapper.transform;
  SportsBroadcastCanvas.Stretch(rect);
  widget.transform.SetParent(rect, false);
  fade = wrapper.GetComponent<CanvasGroup>(); fade.alpha = 0; fade.blocksRaycasts = fade.interactable = false;
  var elements = widget.GetComponentInChildren<GizmoElements>();
  foreach (var graphic in widget.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
  elements.iconImage.sprite = poi.iconNonVisited;
  elements.iconImage.rectTransform.sizeDelta = new Vector2(34,34);
  elements.iconImage.color = new Color(1,.82f,.12f);
  // Rounded, restrained navy surround; the actual POI remains the gold ball.
  var surround = new GameObject("Ball compass surround",typeof(RectTransform),typeof(Image),typeof(SportsBroadcastPlate));
  surround.transform.SetParent(widget.transform,false); surround.transform.SetAsFirstSibling();
  var sr = (RectTransform)surround.transform; sr.sizeDelta = new Vector2(116,138);sr.anchoredPosition=new Vector2(0,-11);
  surround.GetComponent<Image>().color = new Color(.018f,.026f,.048f,.9f);
  surround.GetComponent<Image>().raycastTarget = false;
  surround.GetComponent<SportsBroadcastPlate>().cut = 30;
  elements.distanceText.fontSize = 19; elements.distanceText.color = SportsBroadcastCanvas.Cream;
  elements.distanceText.rectTransform.sizeDelta = new Vector2(112,22);
  elements.distanceText.rectTransform.anchoredPosition = new Vector2(0,-65);
  elements.titleText.gameObject.SetActive(false);
  var arrow=elements.arrowPivot.GetComponentInChildren<Image>();
  arrow.rectTransform.anchoredPosition=new Vector2(36,0);arrow.rectTransform.sizeDelta=new Vector2(30,30);arrow.color=new Color(1,.82f,.12f);
  angleReady = false;
  return widget;
 }
 protected override bool ToggleIndicatorVisibility(CompassProPOI poi,CompassProPOIState state,bool visible) {
  state.indicatorIsVisible = visible;
  // Keep CNP's image alive while the project-owned wrapper fades out.
  if(state.indicatorImage!=null && !state.indicatorImage.gameObject.activeSelf)state.indicatorImage.gameObject.SetActive(true);
  SetVisible(visible);return visible;
 }
 void SetVisible(bool visible) {
  if(shown==visible)return;shown=visible;fading?.Kill();
  if(fade!=null)fading=fade.DOFade(visible?1:0,.18f).SetUpdate(true).SetLink(gameObject);
 }
 protected override void UpdateIndicators() {
  if(!Application.isPlaying)return;
  base.UpdateIndicators();
  if(ballPOI==null||cameraMain==null)return;
  var state=ballPOI.GetState(compassGroup);if(state.indicatorRT==null)return;
  var v=state.viewportPos;
  bool visible=ballPOI.isActiveAndEnabled && v.z>0 && v.x>=0 && v.x<=1 && v.y>=0 && v.y<=1;
  SetVisible(ballPOI.isActiveAndEnabled&&!visible);
  // Vendor GizmoElements arrow points along +X. Bearing comes from CNP's camera-local
  // projection, with actual pixel aspect and a defined directly-behind direction.
  var direction=new Vector2((v.x-.5f)*cameraMain.aspect,v.y-.5f);
  if(v.z<0){direction=-direction;direction.y=-Mathf.Abs(direction.y);}
  if(direction.sqrMagnitude<.000001f)direction=Vector2.down;
  float target=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
  if(!angleReady){bearing=target;angleReady=true;}
  bearing=Mathf.SmoothDampAngle(bearing,target,ref angularVelocity,.1f,720,Time.unscaledDeltaTime);
  state.indicatorArrowRT.localRotation=Quaternion.Euler(0,0,bearing);
  state.indicatorArrowRT.gameObject.SetActive(true);
  state.indicatorRT.localScale=Vector3.one;
  state.indicatorCanvasGroup.alpha=1;
  state.indicatorDistanceText.gameObject.SetActive(true);
  string distance=state.distanceToFollow<1609.344f?SportsMeasure.Feet(state.distanceToFollow):SportsMeasure.Miles(state.distanceToFollow);
  if(state.indicatorDistanceText.text!=distance)state.indicatorDistanceText.text=distance;
  float factor=canvas==null?1:canvas.scaleFactor;
  var pixels=cameraMain.pixelRect;
  var screen=new Vector2(pixels.xMax-78*factor,pixels.yMin+96*factor);
  RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)state.indicatorRT.parent,screen,null,out var local);
  state.indicatorRT.localPosition=new Vector3(local.x,local.y,0);
 }
 public void ResetPresentation(){fading?.Kill();shown=false;angleReady=false;if(fade!=null)fade.alpha=0;}
}
}
