﻿//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2012 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script should be attached to each camera that's used to draw the objects with
/// UI components on them. This may mean only one camera (main camera or your UI camera),
/// or multiple cameras if you happen to have multiple viewports. Failing to attach this
/// script simply means that objects drawn by this camera won't receive UI notifications:
/// 
/// - OnHover (isOver) is sent when the mouse hovers over a collider or moves away.
/// - OnPress (isDown) is sent when a mouse button gets pressed on the collider.
/// - OnSelect (selected) is sent when a mouse button is released on the same object as it was pressed on.
/// - OnClick (int button) is sent with the same conditions as OnSelect, with the added check to see if the mouse has not moved much.
/// - OnDrag (delta) is sent when a mouse or touch gets pressed on a collider and starts dragging it.
/// - OnDrop (gameObject) is sent when the mouse or touch get released on a different collider than the one that was being dragged.
/// - OnInput (text) is sent when typing (after selecting a collider by clicking on it).
/// - OnTooltip (show) is sent when the mouse hovers over a collider for some time without moving.
/// - OnScroll (float delta) is sent out when the mouse scroll wheel is moved.
/// - OnKey (KeyCode key) is sent when keyboard or controller input is used.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Camera")]
[RequireComponent(typeof(Camera))]
public class UICamera : MonoBehaviour
{
	/// <summary>
	/// Whether the touch event will be sending out the OnClick notification at the end.
	/// </summary>

	public enum ClickNotification
	{
		None,
		Always,
		BasedOnDelta,
	}

	/// <summary>
	/// Ambiguous mouse, touch, or controller event.
	/// </summary>

	public class MouseOrTouch
	{
		public Vector2 pos;			// Current position of the mouse or touch event
		public Vector2 delta;		// Delta since last update
		public Vector2 totalDelta;	// Delta since the event started being tracked

		public Camera pressedCam;	// Camera that the OnPress(true) was fired with

		public GameObject current;	// The current game object under the touch or mouse
		public GameObject hover;	// The last game object to receive OnHover
		public GameObject pressed;	// The last game object to receive OnPress

		public ClickNotification clickNotification = ClickNotification.Always;
	}

	class Highlighted
	{
		public GameObject go;
		public int counter = 0;
	}

	/// <summary>
	/// Whether the mouse input is used.
	/// </summary>

	public bool useMouse = true;

	/// <summary>
	/// Whether the touch-based input is used.
	/// </summary>

	public bool useTouch = true;

	/// <summary>
	/// Whether the keyboard events will be processed.
	/// </summary>

	public bool useKeyboard = true;

	/// <summary>
	/// Whether the joystick and controller events will be processed.
	/// </summary>

	public bool useController = true;

	/// <summary>
	/// Which layers will receive events.
	/// </summary>

	public LayerMask eventReceiverMask = -1;

	/// <summary>
	/// How long of a delay to expect before showing the tooltip.
	/// </summary>

	public float tooltipDelay = 1f;

	/// <summary>
	/// How far the mouse is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float mouseClickThreshold = 10f;

	/// <summary>
	/// How far the touch is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float touchClickThreshold = 40f;

	/// <summary>
	/// Raycast range distance. By default it's as far as the camera can see.
	/// </summary>

	public float rangeDistance = -1f;

	/// <summary>
	/// Name of the axis used for scrolling.
	/// </summary>

	public string scrollAxisName = "Mouse ScrollWheel";

	[System.Obsolete("Use UICamera.currentCamera instead")]
	static public Camera lastCamera { get { return currentCamera; } }

	[System.Obsolete("Use UICamera.currentTouchID instead")]
	static public int lastTouchID { get { return currentTouchID; } }

	/// <summary>
	/// Position of the last touch (or mouse) event.
	/// </summary>

	static public Vector2 lastTouchPosition = Vector2.zero;

	/// <summary>
	/// Last raycast hit prior to sending out the event. This is useful if you want detailed information
	/// about what was actually hit in your OnClick, OnHover, and other event functions.
	/// </summary>

	static public RaycastHit lastHit;

	/// <summary>
	/// Last camera active prior to sending out the event. This will always be the camera that actually sent out the event.
	/// </summary>

	static public Camera currentCamera = null;

	/// <summary>
	/// ID of the touch or mouse operation prior to sending out the event. Mouse ID is '-1' for left, '-2' for right mouse button, '-3' for middle.
	/// </summary>

	static public int currentTouchID = -1;

	/// <summary>
	/// Current touch, set before any event function gets called.
	/// </summary>

	static public MouseOrTouch currentTouch = null;

	/// <summary>
	/// If events don't get handled, they will be forwarded to this game object.
	/// </summary>

	static public GameObject fallThrough;

	// List of all active cameras in the scene
	static List<UICamera> mList = new List<UICamera>();

	// List of currently highlighted items
	static List<Highlighted> mHighlighted = new List<Highlighted>();

	// Selected widget (for input)
	static GameObject mSel = null;

	// Mouse event
	static MouseOrTouch mMouse = new MouseOrTouch();

	// Joystick/controller/keyboard event
	static MouseOrTouch mJoystick = new MouseOrTouch();

	// Used to ensure that joystick-based controls don't trigger that often
	static float mNextEvent = 0f;

	// List of currently active touches
	Dictionary<int, MouseOrTouch> mTouches = new Dictionary<int, MouseOrTouch>();

	// Tooltip widget (mouse only)
	GameObject mTooltip = null;

	// Mouse input is turned off on iOS
	Camera mCam = null;
	LayerMask mLayerMask;
	float mTooltipTime = 0f;
	bool mIsEditor = false;

	/// <summary>
	/// Helper function that determines if this script should be handling the events.
	/// </summary>

	bool handlesEvents { get { return eventHandler == this; } }

	/// <summary>
	/// Caching is always preferable for performance.
	/// </summary>

	public Camera cachedCamera { get { if (mCam == null) mCam = camera; return mCam; } }

	/// <summary>
	/// The object the mouse is hovering over. Results may be somewhat odd on touch-based devices.
	/// </summary>

	static public GameObject hoveredObject { get { return mMouse.current; } }

	/// <summary>
	/// Option to manually set the selected game object.
	/// </summary>

	static public GameObject selectedObject
	{
		get
		{
			return mSel;
		}
		set
		{
			if (mSel != value)
			{
				if (mSel != null)
				{
					UICamera uicam = FindCameraForLayer(mSel.layer);
					
					if (uicam != null)
					{
						currentCamera = uicam.mCam;
						mSel.SendMessage("OnSelect", false, SendMessageOptions.DontRequireReceiver);
						Highlight(mSel, false);
					}
				}

				mSel = value;

				if (mSel != null)
				{
					UICamera uicam = FindCameraForLayer(mSel.layer);

					if (uicam != null)
					{
						currentCamera = uicam.mCam;
						Highlight(mSel, true);
						mSel.SendMessage("OnSelect", true, SendMessageOptions.DontRequireReceiver);
					}
				}
			}
		}
	}

	/// <summary>
	/// Clear the list on application quit (also when Play mode is exited)
	/// </summary>

	void OnApplicationQuit () { mHighlighted.Clear(); }

	/// <summary>
	/// Convenience function that returns the main HUD camera.
	/// </summary>

	static public Camera mainCamera
	{
		get
		{
			UICamera mouse = eventHandler;
			return (mouse != null) ? mouse.cachedCamera : null;
		}
	}

	/// <summary>
	/// Event handler for all types of events.
	/// </summary>

	static public UICamera eventHandler
	{
		get
		{
			foreach (UICamera cam in mList)
			{
				// Invalid or inactive entry -- keep going
				if (cam == null || !cam.enabled || !cam.gameObject.active) continue;
				return cam;
			}
			return null;
		}
	}

	/// <summary>
	/// Static comparison function used for sorting.
	/// </summary>

	static int CompareFunc (UICamera a, UICamera b)
	{
		if (a.cachedCamera.depth < b.cachedCamera.depth) return 1;
		if (a.cachedCamera.depth > b.cachedCamera.depth) return -1;
		return 0;
	}

	/// <summary>
	/// Returns the object under the specified position.
	/// </summary>

	static bool Raycast (Vector3 inPos, ref RaycastHit hit)
	{
		foreach (UICamera cam in mList)
		{
			// Skip inactive scripts
			if (!cam.enabled || !cam.gameObject.active) continue;

			// Convert to view space
			currentCamera = cam.cachedCamera;
			Vector3 pos = currentCamera.ScreenToViewportPoint(inPos);

			// If it's outside the camera's viewport, do nothing
			if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f) continue;

			// Cast a ray into the screen
			Ray ray = currentCamera.ScreenPointToRay(inPos);

			// Raycast into the screen
			int mask = currentCamera.cullingMask & (int)cam.eventReceiverMask;
			float dist = (cam.rangeDistance > 0f) ? cam.rangeDistance : currentCamera.farClipPlane - currentCamera.nearClipPlane;
			if (Physics.Raycast(ray, out hit, dist, mask)) return true;
		}
		return false;
	}

	/// <summary>
	/// Find the camera responsible for handling events on objects of the specified layer.
	/// </summary>

	static public UICamera FindCameraForLayer (int layer)
	{
		int layerMask = 1 << layer;

		foreach (UICamera cam in mList)
		{
			Camera uc = cam.cachedCamera;
			if ((uc != null) && (uc.cullingMask & layerMask) != 0) return cam;
		}
		return null;
	}

	/// <summary>
	/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
	/// </summary>

	static int GetDirection (KeyCode up0, KeyCode up1, KeyCode down0, KeyCode down1)
	{
		if (Input.GetKeyDown(up0) || Input.GetKeyDown(up1)) return 1;
		if (Input.GetKeyDown(down0) || Input.GetKeyDown(down1)) return -1;
		return 0;
	}

	/// <summary>
	/// Using the joystick to move the UI results in 1 or -1 if the threshold has been passed, mimicking up/down keys.
	/// </summary>

	static int GetDirection (string axis)
	{
		float time = Time.realtimeSinceStartup;

		if (mNextEvent < time)
		{
			float val = Input.GetAxis(axis);

			if (val > 0.75f)
			{
				mNextEvent = time + 0.25f;
				return 1;
			}

			if (val < -0.75f)
			{
				mNextEvent = time + 0.25f;
				return -1;
			}
		}
		return 0;
	}

	/// <summary>
	/// Apply or remove highlighted (hovered) state from the specified object.
	/// </summary>

	static void Highlight (GameObject go, bool highlighted)
	{
		if (go != null)
		{
			for (int i = mHighlighted.Count; i > 0; )
			{
				Highlighted hl = mHighlighted[--i];

				if (hl == null || hl.go == null)
				{
					mHighlighted.RemoveAt(i);
				}
				else if (hl.go == go)
				{
					if (highlighted)
					{
						++hl.counter;
					}
					else if (--hl.counter < 1)
					{
						mHighlighted.Remove(hl);
						go.SendMessage("OnHover", false, SendMessageOptions.DontRequireReceiver);
					}
					return;
				}
			}

			if (highlighted)
			{
				Highlighted hl = new Highlighted();
				hl.go = go;
				hl.counter = 1;
				mHighlighted.Add(hl);
				go.SendMessage("OnHover", true, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	/// <summary>
	/// Get or create a touch event.
	/// </summary>

	MouseOrTouch GetTouch (int id)
	{
		MouseOrTouch touch;

		if (!mTouches.TryGetValue(id, out touch))
		{
			touch = new MouseOrTouch();
			mTouches.Add(id, touch);
		}
		return touch;
	}

	/// <summary>
	/// Remove a touch event from the list.
	/// </summary>

	void RemoveTouch (int id)
	{
		mTouches.Remove(id);
	}

	/// <summary>
	/// Add this camera to the list.
	/// </summary>

	void Awake ()
	{
		if (Application.platform == RuntimePlatform.Android ||
			Application.platform == RuntimePlatform.IPhonePlayer)
		{
			useMouse = false;
			useTouch = true;
			useKeyboard = false;
			useController = false;
		}
		else if (Application.platform == RuntimePlatform.PS3 ||
				 Application.platform == RuntimePlatform.XBOX360)
		{
			useMouse = false;
			useTouch = false;
			useKeyboard = false;
			useController = true;
		}
		else if (Application.platform == RuntimePlatform.WindowsEditor ||
				 Application.platform == RuntimePlatform.OSXEditor)
		{
			mIsEditor = true;
		}

		// Save the starting mouse position
		mMouse.pos.x = Input.mousePosition.x;
		mMouse.pos.y = Input.mousePosition.y;
		lastTouchPosition = mMouse.pos;

		// Add this camera to the list
		mList.Add(this);
		mList.Sort(CompareFunc);

		// If no event receiver mask was specified, use the camera's mask
		if (eventReceiverMask == -1) eventReceiverMask = camera.cullingMask;
	}

	/// <summary>
	/// Remove this camera from the list.
	/// </summary>

	void OnDestroy ()
	{
		mList.Remove(this);
	}

	/// <summary>
	/// Update the object under the mouse if we're not using touch-based input.
	/// </summary>

	void FixedUpdate ()
	{
		if (useMouse && Application.isPlaying && handlesEvents)
		{
			mMouse.current = Raycast(Input.mousePosition, ref lastHit) ? lastHit.collider.gameObject : fallThrough;
		}
	}

	/// <summary>
	/// Check the input and send out appropriate events.
	/// </summary>

	void Update ()
	{
		// Only the first UI layer should be processing events
		if (!Application.isPlaying || !handlesEvents) return;

		// Update mouse input
		if (useMouse || (useTouch && mIsEditor)) ProcessMouse();

		// Process touch input
		if (useTouch) ProcessTouches();

		// Clear the selection on escape
		if (useKeyboard && mSel != null && Input.GetKeyDown(KeyCode.Escape)) selectedObject = null;

		// Forward the input to the selected object
		if (mSel != null)
		{
			string input = Input.inputString;

			// Adding support for some macs only having the "Delete" key instead of "Backspace"
			if (useKeyboard && Input.GetKeyDown(KeyCode.Delete)) input += "\b";

			if (input.Length > 0)
			{
				if (mTooltip != null) ShowTooltip(false);
				mSel.SendMessage("OnInput", input, SendMessageOptions.DontRequireReceiver);
			}

			// Update the keyboard and joystick events
			ProcessOthers();
		}

		// If it's time to show a tooltip, inform the object we're hovering over
		if (useMouse && mMouse.hover != null)
		{
			float scroll = Input.GetAxis(scrollAxisName);
			if (scroll != 0f) mMouse.hover.SendMessage("OnScroll", scroll, SendMessageOptions.DontRequireReceiver);

			if (mTooltipTime != 0f && mTooltipTime < Time.realtimeSinceStartup)
			{
				mTooltip = mMouse.hover;
				ShowTooltip(true);
			}
		}
	}

	/// <summary>
	/// Update mouse input.
	/// </summary>

	void ProcessMouse ()
	{
		currentTouch = mMouse;
		lastTouchPosition = Input.mousePosition;
		currentTouch.delta = lastTouchPosition - currentTouch.pos;

		bool updateRaycast = (Time.timeScale < 0.9f);

		if (!updateRaycast)
		{
			for (int i = 0; i < 3; ++i)
			{
				if (Input.GetMouseButton(i) || Input.GetMouseButtonUp(i))
				{
					updateRaycast = true;
					break;
				}
			}
		}

		if (updateRaycast)
		{
			currentTouch.current = Raycast(lastTouchPosition, ref lastHit) ? lastHit.collider.gameObject : fallThrough;
		}

		for (int i = 0; i < 3; ++i)
		{
			bool pressed = Input.GetMouseButtonDown(i);
			bool unpressed = Input.GetMouseButtonUp(i);

			currentTouchID = -1 - i;

			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;

			if (i == 0 && currentTouch.pos != lastTouchPosition)
			{
				if (mTooltipTime != 0f) mTooltipTime = Time.realtimeSinceStartup + tooltipDelay;
				else if (mTooltip != null) ShowTooltip(false);
				currentTouch.pos = lastTouchPosition;
			}

			// Process the mouse events
			ProcessTouch(pressed, unpressed);
		}
		currentTouch = null;
	}

	/// <summary>
	/// Update touch-based events.
	/// </summary>

	void ProcessTouches ()
	{
		for (int i = 0; i < Input.touchCount; ++i)
		{
			Touch input = Input.GetTouch(i);
			currentTouchID = input.fingerId;
			currentTouch = GetTouch(currentTouchID);

			bool pressed = (input.phase == TouchPhase.Began);
			bool unpressed = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);

			if (pressed)
			{
				currentTouch.delta = Vector2.zero;
			}
			else
			{
				// Although input.deltaPosition can be used, calculating it manually is safer (just in case)
				currentTouch.delta = input.position - currentTouch.pos;
			}

			currentTouch.pos = input.position;
			currentTouch.current = Raycast(currentTouch.pos, ref lastHit) ? lastHit.collider.gameObject : fallThrough;
			lastTouchPosition = currentTouch.pos;

			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;

			// Process the events from this touch
			ProcessTouch(pressed, unpressed);

			// If the touch has ended, remove it from the list
			if (unpressed) RemoveTouch(currentTouchID);
			currentTouch = null;
		}
	}

	/// <summary>
	/// Process keyboard and joystick events.
	/// </summary>

	void ProcessOthers ()
	{
		currentTouchID = -100;
		currentTouch = mJoystick;

		// Enter key and joystick button 1 keys are treated the same -- as a "click"
		bool returnKeyDown	= (useKeyboard	 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)));
		bool buttonKeyDown	= (useController &&  Input.GetKeyDown(KeyCode.JoystickButton0));
		bool returnKeyUp	= (useKeyboard	 && (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space)));
		bool buttonKeyUp	= (useController &&  Input.GetKeyUp(KeyCode.JoystickButton0));

		bool down	= returnKeyDown || buttonKeyDown;
		bool up		= returnKeyUp || buttonKeyUp;

		if (down || up)
		{
			currentTouch.hover = mSel;
			currentTouch.current = mSel;
			ProcessTouch(down, up);
		}

		int vertical = 0;
		int horizontal = 0;

		if (useKeyboard)
		{
			vertical += GetDirection(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow);
			horizontal += GetDirection(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
		}

		if (useController)
		{
			vertical += GetDirection("Vertical");
			horizontal += GetDirection("Horizontal");
		}

		// Send out key notifications
		if (vertical != 0) mSel.SendMessage("OnKey", vertical > 0 ? KeyCode.UpArrow : KeyCode.DownArrow, SendMessageOptions.DontRequireReceiver);
		if (horizontal != 0) mSel.SendMessage("OnKey", horizontal > 0 ? KeyCode.RightArrow : KeyCode.LeftArrow, SendMessageOptions.DontRequireReceiver);
		if (useKeyboard && Input.GetKeyDown(KeyCode.Tab)) mSel.SendMessage("OnKey", KeyCode.Tab, SendMessageOptions.DontRequireReceiver);
		if (useController && Input.GetKeyUp(KeyCode.JoystickButton1)) mSel.SendMessage("OnKey", KeyCode.Escape, SendMessageOptions.DontRequireReceiver);

		currentTouch = null;
	}

	/// <summary>
	/// Process the events of the specified touch.
	/// </summary>

	void ProcessTouch (bool pressed, bool unpressed)
	{
		// If we're using the mouse for input, we should send out a hover(false) message first
		if (currentTouch.pressed == null && currentTouch.hover != currentTouch.current && currentTouch.hover != null)
		{
			if (mTooltip != null) ShowTooltip(false);
			Highlight(currentTouch.hover, false);
		}

		// Send out the press message
		if (pressed)
		{
			if (mTooltip != null) ShowTooltip(false);
			currentTouch.pressed = currentTouch.current;
			currentTouch.clickNotification = ClickNotification.Always;
			currentTouch.totalDelta = Vector2.zero;
			if (currentTouch.pressed != null) currentTouch.pressed.SendMessage("OnPress", true, SendMessageOptions.DontRequireReceiver);

			// Clear the selection
			if (currentTouch.pressed != mSel)
			{
				if (mTooltip != null) ShowTooltip(false);
				selectedObject = null;
			}
		}
		else if (currentTouch.pressed != null && currentTouch.delta.magnitude != 0f)
		{
			if (mTooltip != null) ShowTooltip(false);
			currentTouch.totalDelta += currentTouch.delta;

			bool isDisabled = (currentTouch.clickNotification == ClickNotification.None);
			currentTouch.pressed.SendMessage("OnDrag", currentTouch.delta, SendMessageOptions.DontRequireReceiver);

			if (isDisabled)
			{
				// If the notification status has already been disabled, keep it as such
				currentTouch.clickNotification = ClickNotification.None;
			}
			else if (currentTouch.clickNotification == ClickNotification.BasedOnDelta)
			{
				// If the notification is based on delta and the delta gets exceeded, disable the notification
				float threshold = (currentTouch == mMouse) ? mouseClickThreshold : Mathf.Max(touchClickThreshold, Screen.height * 0.1f);

				if (currentTouch.totalDelta.magnitude > threshold)
				{
					currentTouch.clickNotification = ClickNotification.None;
				}
			}
		}

		// Send out the unpress message
		if (unpressed)
		{
			if (mTooltip != null) ShowTooltip(false);

			if (currentTouch.pressed != null)
			{
				currentTouch.pressed.SendMessage("OnPress", false, SendMessageOptions.DontRequireReceiver);

				// Send a hover message to the object, but don't add it to the list of hovered items as it's already present
				if (currentTouch.pressed == currentTouch.hover) currentTouch.pressed.SendMessage("OnHover", true, SendMessageOptions.DontRequireReceiver);

				// If the button/touch was released on the same object, consider it a click and select it
				if (currentTouch.pressed == currentTouch.current)
				{
					if (currentTouch.pressed != mSel)
					{
						mSel = currentTouch.pressed;
						currentTouch.pressed.SendMessage("OnSelect", true, SendMessageOptions.DontRequireReceiver);
					}
					else
					{
						mSel = currentTouch.pressed;
					}

					// If the touch should consider clicks, send out an OnClick notification
					if (currentTouch.clickNotification != ClickNotification.None)
					{
						currentTouch.pressed.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
					}
				}
				else // The button/touch was released on a different object
				{
					// Send a drop notification (for drag & drop)
					if (currentTouch.current != null) currentTouch.current.SendMessage("OnDrop", currentTouch.pressed, SendMessageOptions.DontRequireReceiver);

					// If we're using mouse-based input, send a hover notification
					Highlight(currentTouch.pressed, false);
				}
			}
			currentTouch.pressed = null;
		}

		// Send out a hover(true) message last
		if (useMouse && currentTouch.pressed == null && currentTouch.hover != currentTouch.current)
		{
			mTooltipTime = Time.realtimeSinceStartup + tooltipDelay;
			currentTouch.hover = currentTouch.current;
			Highlight(currentTouch.hover, true);
		}
	}

	/// <summary>
	/// Show or hide the tooltip.
	/// </summary>

	void ShowTooltip (bool val)
	{
		mTooltipTime = 0f;
		if (mTooltip != null) mTooltip.SendMessage("OnTooltip", val, SendMessageOptions.DontRequireReceiver);
		if (!val) mTooltip = null;
	}
}