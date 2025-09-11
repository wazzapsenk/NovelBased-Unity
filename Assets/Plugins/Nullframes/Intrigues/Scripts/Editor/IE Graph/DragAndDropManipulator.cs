using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues.EDITOR
{
    // DragAndDropManipulator is a manipulator that stores pointer-related callbacks, so it inherits from
    // PointerManipulator.
    internal class DragAndDropManipulator : PointerManipulator
    {
        // The stored asset object, if any.
        private Object droppedObject;

        // The path of the stored asset, or the empty string if there isn't one.
        private string assetPath = string.Empty;

        private Type currentType;

        private Action<string[]> Perform;

        public DragAndDropManipulator(VisualElement root, Type type, Action<string[]> perform)
        {
            // The target of the manipulator, the object to which to register all callbacks, is the drop area.
            target = root;
            currentType = type;
            Perform = perform;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // Register a callback when the user presses the pointer down.
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            // Register callbacks for various stages in the drag process.
            target.RegisterCallback<DragEnterEvent>(OnDragEnter);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            // Unregister all callbacks that you registered in RegisterCallbacksOnTarget().
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        }

        // This method runs when a user presses a pointer down on the drop area.
        private void OnPointerDown(PointerDownEvent _)
        {
            // Only do something if the window currently has a reference to an asset object.
            if (droppedObject != null)
            {
                // Clear existing data in DragAndDrop class.
                DragAndDrop.PrepareStartDrag();

                // Store reference to object and path to object in DragAndDrop static fields.
                DragAndDrop.objectReferences = new[] { droppedObject };
                if (assetPath != string.Empty)
                    DragAndDrop.paths = new[] { assetPath };
                else
                    DragAndDrop.paths = new string[] { };

                // Start a drag.
                DragAndDrop.StartDrag(string.Empty);
            }
        }

        // This method runs if a user brings the pointer over the target while a drag is in progress.
        private void OnDragEnter(DragEnterEvent _)
        {
            // Get the name of the object the user is dragging.
            if (DragAndDrop.paths.Length > 0)
            {
                assetPath = DragAndDrop.paths[0];
                var splitPath = assetPath.Split('/');
            }
            else if (DragAndDrop.objectReferences.Length > 0) { }
        }

        // This method runs if a user makes the pointer leave the bounds of the target while a drag is in progress.
        private void OnDragLeave(DragLeaveEvent _)
        {
            assetPath = string.Empty;
            droppedObject = null;
        }

        // This method runs every frame while a drag is in progress.
        private void OnDragUpdate(DragUpdatedEvent _)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                var obj = DragAndDrop.objectReferences[0];

                if (obj is Texture2D texture2D)
                {
                    var type = texture2D.GetTextureType();
                    if (type is not TextureImporterType.Sprite) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                if (obj.GetType() != currentType) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        // This method runs when a user drops a dragged object onto the target.
        private void OnDragPerform(DragPerformEvent _)
        {
            // Set droppedObject and draggedName fields to refer to dragged object.
            Perform.Invoke(DragAndDrop.paths);
        }
    }
}