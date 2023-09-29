﻿
using Editor;
using Sandbox;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Editor.BaseItemWidget;

public partial class GameObjectNode : TreeNode<GameObject>
{
	public GameObjectNode( GameObject o ) : base ( o )
	{
		Height = 18;
	}

	public override bool HasChildren => Value.Children.Any();

	protected override void BuildChildren()
	{
		SetChildren( Value.Children, x => new GameObjectNode( x ) );
	}

	public override int ValueHash
	{
		get
		{
			HashCode hc = new HashCode();
			hc.Add( Value.Name );

			foreach ( var val in Value.Children )
			{
				hc.Add( val );
			}

			return hc.ToHashCode();
		}
	}

	public override void OnPaint( VirtualWidget item )
	{
		var selected = item.Selected || item.Pressed || item.Dragging;

		var fullSpanRect = item.Rect;
		fullSpanRect.Left = 0;
		fullSpanRect.Right = TreeView.Width;

		float opacity = 0.9f;

		if ( !Value.Active ) opacity *= 0.5f;

		//
		// If there's a drag and drop happening, fade out nodes that aren't possible
		//
		if ( TreeView.IsBeingDroppedOn && (TreeView.CurrentItemDragEvent.Data.Object is not GameObject go || Value.IsAncestor( go ) )  )
		{
			opacity *= 0.23f;
		}

		if ( item.Dropping )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.Blue.WithAlpha( 0.2f ) );

			if( TreeView.CurrentItemDragEvent.DropEdge.HasFlag( ItemEdge.Top ) )
			{
				var droprect = item.Rect;
				droprect.Top -= 1;
				droprect.Height = 2;
				Paint.DrawRect( droprect, 2 );
			}
			else if ( TreeView.CurrentItemDragEvent.DropEdge.HasFlag( ItemEdge.Bottom ) )
			{
				var droprect = item.Rect;
				droprect.Top = droprect.Bottom - 1;
				droprect.Height = 2;
				Paint.DrawRect( droprect, 2 );
			}
			else
			{
				Paint.DrawRect( item.Rect, 2 );
			}
		}

		if ( selected )
		{
			//item.PaintBackground( Color.Transparent, 3 );
			Paint.ClearPen();
			Paint.SetBrush( Theme.Blue.WithAlpha( 0.4f * opacity ) );
			Paint.DrawRect( fullSpanRect );

			Paint.SetPen( Color.White.WithAlpha( opacity ) );
		}
		else
		{
			Paint.SetPen( Theme.ControlText.WithAlpha( opacity ) );
		}

		var name = Value.Name;
		if ( string.IsNullOrWhiteSpace( name ) ) name = "Untitled GameObject";

		var r = item.Rect;
		r.Left += 4;
		 
		if ( !selected ) Paint.SetPen( Theme.Blue.WithAlpha( opacity ).Saturate( opacity - 1.0f ) );
		Paint.DrawIcon( r, "circle", 14, TextFlag.LeftCenter );
		r.Left += 22;

		Paint.SetPen( selected ? Theme.White.WithAlpha( opacity ) : Theme.ControlText.WithAlpha( opacity ) );
		Paint.SetDefaultFont( 9 );
		Paint.DrawText( r, name, TextFlag.LeftCenter );
	}

	public override bool OnDragStart()
	{
		var drag = new Drag( TreeView );
		drag.Data.Object = Value;
		drag.Execute();

		return true;
	}

	public override DropAction OnDragDrop( ItemDragEvent e )
	{
		if ( e.Data.Object is GameObject go )
		{
			// can't parent to an ancesor
			if ( go == Value || Value.IsAncestor( go ) )
				return DropAction.Ignore;

			if ( e.IsDrop )
			{
				if ( e.DropEdge.HasFlag( ItemEdge.Top ) )
				{
					Value.AddSibling( go, true );
				}
				else if ( e.DropEdge.HasFlag( ItemEdge.Bottom ) )
				{
					Value.AddSibling( go, false );
				}
				else
				{
					go.Parent = Value;
				}
			}

			return DropAction.Move;
		}

		return DropAction.Ignore;
	}

	public override bool OnContextMenu()
	{
		var m = new Menu();

		m.AddOption( "Cut", action: Cut );
		m.AddOption( "Copy", action: Copy );
		m.AddOption( "Paste", action: Paste );
		m.AddOption( "Paste As Child", action: PasteAsChild );
		m.AddSeparator();
		//m.AddOption( "rename", action: Delete );
		//m.AddOption( "duplicate", action: Delete );
		m.AddOption( "Delete", action: Delete );

		m.AddSeparator();

		CreateObjectMenu( m, go =>
		{
			go.Parent = Value;
			TreeView.Open( this );
			TreeView.SelectItem( go );
		} );

		// cut
		// copy
		// paste 
		// paste as child
		// --
		// rename
		// duplicate
		// delete

		m.AddSeparator();
		m.AddOption( "Properties..", action: OpenPropertyWindow );

		m.OpenAtCursor();

		return true;
	}

	void Cut()
	{
		Copy();
		Delete();
	}

	void Copy()
	{
		var json = Value.Serialize();
		EditorUtility.Clipboard.Copy( json.ToString() );
	}

	void Paste()
	{
		var text = EditorUtility.Clipboard.Paste();
		if ( JsonNode.Parse( text ) is JsonObject jso )
		{
			var go = Value.Scene.CreateObject();
			go.Deserialize( jso );
			go.Parent = Value.Parent;

			TreeView.SelectItem( go );
		}
	}

	void PasteAsChild()
	{
		var text = EditorUtility.Clipboard.Paste();
		if ( JsonNode.Parse( text ) is JsonObject jso )
		{
			var go = new GameObject();
			go.Deserialize( jso );
			go.Parent = Value;

			TreeView.Open( this );
			TreeView.SelectItem( go );
		}
	}

	void Delete()
	{
		Value.Destroy();
	}

	void OpenPropertyWindow()
	{

	}

	public static void CreateObjectMenu( Menu menu, Action<GameObject> then )
	{
		menu.AddOption( "Create Empty", "category", () =>
		{
			var go = new GameObject();
			go.Name = "Object";
			then( go );
		} );

		// 3d obj
		{
			var submenu = menu.AddMenu( "3D Object" );

			submenu.AddOption( "Cube", "category", () =>
			{
				var go = new GameObject();
				go.Name = "Cube";

				var model = go.AddComponent<ModelComponent>();
				model.Model = Model.Load( "models/dev/box.vmdl" );

				then( go );
			} );

			submenu.AddOption( "Sphere", "category", () =>
			{
				var go = new GameObject();
				go.Name = "Sphere";

				var model = go.AddComponent<ModelComponent>();
				model.Model = Model.Load( "models/dev/sphere.vmdl" );

				then( go );
			} );


			submenu.AddOption( "Plane", "category", () =>
			{
				var go = new GameObject();
				go.Name = "Plane";

				var model = go.AddComponent<ModelComponent>();
				model.Model = Model.Load( "models/dev/plane.vmdl" );

				then( go );
			} );
		}

		{
			menu.AddOption( "Camera", "category", () =>
			{
				var go = new GameObject();
				go.Name = "Camera";

				var cam = go.AddComponent<CameraComponent>();

				then( go );
			} );

		}
	}
}

