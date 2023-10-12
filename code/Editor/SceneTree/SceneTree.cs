﻿[Dock( "Editor", "Hierarchy", "list" )]
public partial class SceneTreeWidget : Widget
{
	TreeView TreeView;

	Layout Header;
	Layout Footer;

	public SceneTreeWidget( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();

		BuildUI();
	}

	[Event.Hotload]
	public void BuildUI()
	{
		Layout.Clear( true );
		Layout.Add( new OpenSceneList( this ) );
		Header = Layout.AddColumn();
		Header = Layout.AddColumn();
		TreeView = Layout.Add( new TreeView( this ), 1 );
		TreeView.MultiSelect = true;
		TreeView.Selection = EditorScene.Selection;
		Footer = Layout.AddColumn();
		_lastScene = null;
		CheckForChanges();
	}

	SceneEditorSession _lastScene;

	[EditorEvent.Frame]
	public void CheckForChanges()
	{
		var activeScene = SceneEditorSession.Active;

		if ( _lastScene == activeScene ) return;

		_lastScene = activeScene;

		// Copy the current selection as we're about to kill it
		var selection = new List<object>( EditorScene.Selection );

		Header.Clear( true );
		TreeView.Clear();

		if ( _lastScene is null )
			return;

		if ( _lastScene is not GameEditorSession && _lastScene.Scene is PrefabScene prefabScene )
		{
			var node = TreeView.AddItem( new PrefabNode( prefabScene ) );
			TreeView.Open( node );

			return;
		}
		else
		{
			var node = TreeView.AddItem( new SceneNode( _lastScene.Scene ) );
			TreeView.Open( node );
		}

		// Iterate through selection, try to find them in the new tree
		foreach ( var go in selection.Select( x => x as GameObject ) )
		{
			if ( activeScene.Scene.FindObjectByGuid( go.Id ) is GameObject activeObj )
				TreeView.Selection.Add( activeObj );
		}
	}
}

