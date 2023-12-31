﻿//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2012 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// UI Atlas contains a collection of sprites inside one large texture atlas.
/// </summary>

[AddComponentMenu("NGUI/UI/Atlas")]
public class UIAtlas : MonoBehaviour
{
	[System.Serializable]
	public class Sprite
	{
		public string name = "Unity Bug";
		public Rect outer = new Rect(0f, 0f, 1f, 1f);
		public Rect inner = new Rect(0f, 0f, 1f, 1f);

		// Padding is needed for trimmed sprites and is relative to sprite width and height
		public float paddingLeft	= 0f;
		public float paddingRight	= 0f;
		public float paddingTop		= 0f;
		public float paddingBottom	= 0f;

		public bool hasPadding { get { return paddingLeft != 0f || paddingRight != 0f || paddingTop != 0f || paddingBottom != 0f; } }
	}

	/// <summary>
	/// Pixels coordinates are values within the texture specified in pixels. They are more intuitive,
	/// but will likely change if the texture gets resized. TexCoord coordinates range from 0 to 1,
	/// and won't change if the texture is resized. You can switch freely from one to the other prior
	/// to modifying the texture used by the atlas.
	/// </summary>

	public enum Coordinates
	{
		Pixels,
		TexCoords,
	}

	// Material used by this atlas. Name is kept only for backwards compatibility, it used to be public.
	[SerializeField] Material material;

	// List of all sprites inside the atlas. Name is kept only for backwards compatibility, it used to be public.
	[SerializeField] List<Sprite> sprites = new List<Sprite>();

	// Currently active set of coordinates
	[SerializeField] Coordinates mCoordinates = Coordinates.Pixels;

	// Size in pixels for the sake of MakePixelPerfect functions.
	[SerializeField] float mPixelSize = 1f;

	// Replacement atlas can be used to completely bypass this atlas, pulling the data from another one instead.
	[SerializeField] UIAtlas mReplacement;

	/// <summary>
	/// Material used by the atlas.
	/// </summary>

	public Material spriteMaterial
	{
		get
		{
			return (mReplacement != null) ? mReplacement.spriteMaterial : material;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.spriteMaterial = value;
			}
			else
			{
				if (material == null)
				{
					material = value;
				}
				else
				{
					MarkAsDirty();
					material = value;
					MarkAsDirty();
				}
			}
		}
	}

	/// <summary>
	/// List of sprites within the atlas.
	/// </summary>

	public List<Sprite> spriteList
	{
		get
		{
			return (mReplacement != null) ? mReplacement.spriteList : sprites;
		}
		set
		{
			if (mReplacement != null) mReplacement.spriteList = value;
			else sprites = value;
		}
	}

	/// <summary>
	/// Texture used by the atlas.
	/// </summary>

	public Texture texture { get { return (mReplacement != null) ? mReplacement.texture : (material != null ? material.mainTexture as Texture : null); } }

	/// <summary>
	/// Allows switching of the coordinate system from pixel coordinates to texture coordinates.
	/// </summary>

	public Coordinates coordinates
	{
		get
		{
			return (mReplacement != null) ? mReplacement.coordinates : mCoordinates;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.coordinates = value;
			}
			else if (mCoordinates != value)
			{
				if (material == null || material.mainTexture == null)
				{
					Debug.LogError("Can't switch coordinates until the atlas material has a valid texture");
					return;
				}

				mCoordinates = value;
				Texture tex = material.mainTexture;

				foreach (Sprite s in sprites)
				{
					if (mCoordinates == Coordinates.TexCoords)
					{
						s.outer = NGUIMath.ConvertToTexCoords(s.outer, tex.width, tex.height);
						s.inner = NGUIMath.ConvertToTexCoords(s.inner, tex.width, tex.height);
					}
					else
					{
						s.outer = NGUIMath.ConvertToPixels(s.outer, tex.width, tex.height, true);
						s.inner = NGUIMath.ConvertToPixels(s.inner, tex.width, tex.height, true);
					}
				}
			}
		}
	}

	/// <summary>
	/// Pixel size is a multiplier applied to widgets dimensions when performing MakePixelPerfect() pixel correction.
	/// Most obvious use would be on retina screen displays. The resolution doubles, but with UIRoot staying the same
	/// for layout purposes, you can still get extra sharpness by switching to an HD atlas that has pixel size set to 0.5.
	/// </summary>

	public float pixelSize
	{
		get
		{
			return (mReplacement != null) ? mReplacement.pixelSize : mPixelSize;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.pixelSize = value;
			}
			else
			{
				float val = Mathf.Clamp(value, 0.25f, 4f);

				if (mPixelSize != val)
				{
					mPixelSize = val;
					MarkAsDirty();
				}
			}
		}
	}

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this atlas to use the replacement atlas instead.
	/// Suggested use: set up all your widgets to use a dummy atlas that points to the real atlas. Switching that atlas
	/// to another one (for example an HD atlas) is then a simple matter of setting this field on your dummy atlas.
	/// </summary>

	public UIAtlas replacement
	{
		get
		{
			return mReplacement;
		}
		set
		{
			UIAtlas rep = value;
			if (rep == this) rep = null;

			if (mReplacement != rep)
			{
				if (rep != null && rep.replacement == this) rep.replacement = null;
				if (mReplacement != null) MarkAsDirty();
				mReplacement = rep;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Convenience function that retrieves a sprite by name.
	/// </summary>

	public Sprite GetSprite (string name)
	{
		if (mReplacement != null)
		{
			return mReplacement.GetSprite(name);
		}
		else if (!string.IsNullOrEmpty(name))
		{
			foreach (Sprite s in sprites)
			{
				// string.Equals doesn't seem to work with Flash export
				if (!string.IsNullOrEmpty(s.name) && name == s.name)
				{
					return s;
				}
			}
		}
		else
		{
			Debug.LogWarning("Expected a valid name, found nothing");
		}
		return null;
	}

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names.
	/// </summary>

	public List<string> GetListOfSprites ()
	{
		if (mReplacement != null) return mReplacement.GetListOfSprites();
		List<string> list = new List<string>();
		foreach (Sprite s in sprites) if (s != null && !string.IsNullOrEmpty(s.name)) list.Add(s.name);
		list.Sort();
		return list;
	}

	/// <summary>
	/// Helper function that determines whether the atlas uses the specified one, taking replacements into account.
	/// </summary>

	bool References (UIAtlas atlas)
	{
		if (atlas == null) return false;
		if (atlas == this) return true;
		return (mReplacement != null) ? mReplacement.References(atlas) : false;
	}

	/// <summary>
	/// Helper function that determines whether the two atlases are related.
	/// </summary>

	static public bool CheckIfRelated (UIAtlas a, UIAtlas b)
	{
		if (a == null || b == null) return false;
		return a == b || a.References(b) || b.References(a);
	}

	/// <summary>
	/// Mark all widgets associated with this atlas as having changed.
	/// </summary>

	public void MarkAsDirty ()
	{
		UISprite[] list = NGUITools.FindActive<UISprite>();

		foreach (UISprite sp in list)
		{
			if (CheckIfRelated(this, sp.atlas))
			{
				UIAtlas atl = sp.atlas;
				sp.atlas = null;
				sp.atlas = atl;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(sp);
#endif
			}
		}

		UIFont[] fonts = Resources.FindObjectsOfTypeAll(typeof(UIFont)) as UIFont[];

		foreach (UIFont font in fonts)
		{
			if (CheckIfRelated(this, font.atlas))
			{
				UIAtlas atl = font.atlas;
				font.atlas = null;
				font.atlas = atl;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(font);
#endif
			}
		}

		UILabel[] labels = NGUITools.FindActive<UILabel>();

		foreach (UILabel lbl in labels)
		{
			if (lbl.font != null && CheckIfRelated(this, lbl.font.atlas))
			{
				UIFont font = lbl.font;
				lbl.font = null;
				lbl.font = font;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(lbl);
#endif
			}
		}
	}
}