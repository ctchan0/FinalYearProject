//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.3.0
//     from Assets/InputActions/MatchControls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @MatchControls : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @MatchControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""MatchControls"",
    ""maps"": [
        {
            ""name"": ""Piece"",
            ""id"": ""7c300c2a-3d2f-4b60-8c15-e4dbe519030c"",
            ""actions"": [
                {
                    ""name"": ""MoveLeft"",
                    ""type"": ""Value"",
                    ""id"": ""8e51dd16-1da0-44a4-a924-0b5a434f5c82"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""MoveRight"",
                    ""type"": ""Value"",
                    ""id"": ""dab32cb2-79c9-4da5-8dfe-c868e6b85ff3"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""RotateAnticlockwise"",
                    ""type"": ""Value"",
                    ""id"": ""cf0bac21-1974-487c-9599-5605cd65cd98"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""RotateClockwise"",
                    ""type"": ""Value"",
                    ""id"": ""94fcf7f8-3c6c-4140-bbb6-1534007bfa01"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""MoveDown"",
                    ""type"": ""Value"",
                    ""id"": ""bafc2928-f16c-454b-b6a5-01b15bca1346"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""46309e1c-aaf4-4742-b60d-066b843ba901"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5e99ca2b-aaa5-4cec-85ed-320bef82c3f6"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""88dd09c6-de28-4b87-8c2a-af217e235042"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RotateAnticlockwise"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2ccfb891-cbba-4820-a28f-5d0c29068f74"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""41ec3376-b810-4671-bc72-753c664d76ac"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RotateClockwise"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Piece
        m_Piece = asset.FindActionMap("Piece", throwIfNotFound: true);
        m_Piece_MoveLeft = m_Piece.FindAction("MoveLeft", throwIfNotFound: true);
        m_Piece_MoveRight = m_Piece.FindAction("MoveRight", throwIfNotFound: true);
        m_Piece_RotateAnticlockwise = m_Piece.FindAction("RotateAnticlockwise", throwIfNotFound: true);
        m_Piece_RotateClockwise = m_Piece.FindAction("RotateClockwise", throwIfNotFound: true);
        m_Piece_MoveDown = m_Piece.FindAction("MoveDown", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Piece
    private readonly InputActionMap m_Piece;
    private IPieceActions m_PieceActionsCallbackInterface;
    private readonly InputAction m_Piece_MoveLeft;
    private readonly InputAction m_Piece_MoveRight;
    private readonly InputAction m_Piece_RotateAnticlockwise;
    private readonly InputAction m_Piece_RotateClockwise;
    private readonly InputAction m_Piece_MoveDown;
    public struct PieceActions
    {
        private @MatchControls m_Wrapper;
        public PieceActions(@MatchControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @MoveLeft => m_Wrapper.m_Piece_MoveLeft;
        public InputAction @MoveRight => m_Wrapper.m_Piece_MoveRight;
        public InputAction @RotateAnticlockwise => m_Wrapper.m_Piece_RotateAnticlockwise;
        public InputAction @RotateClockwise => m_Wrapper.m_Piece_RotateClockwise;
        public InputAction @MoveDown => m_Wrapper.m_Piece_MoveDown;
        public InputActionMap Get() { return m_Wrapper.m_Piece; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PieceActions set) { return set.Get(); }
        public void SetCallbacks(IPieceActions instance)
        {
            if (m_Wrapper.m_PieceActionsCallbackInterface != null)
            {
                @MoveLeft.started -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveLeft;
                @MoveLeft.performed -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveLeft;
                @MoveLeft.canceled -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveLeft;
                @MoveRight.started -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveRight;
                @MoveRight.performed -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveRight;
                @MoveRight.canceled -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveRight;
                @RotateAnticlockwise.started -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateAnticlockwise;
                @RotateAnticlockwise.performed -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateAnticlockwise;
                @RotateAnticlockwise.canceled -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateAnticlockwise;
                @RotateClockwise.started -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateClockwise;
                @RotateClockwise.performed -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateClockwise;
                @RotateClockwise.canceled -= m_Wrapper.m_PieceActionsCallbackInterface.OnRotateClockwise;
                @MoveDown.started -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveDown;
                @MoveDown.performed -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveDown;
                @MoveDown.canceled -= m_Wrapper.m_PieceActionsCallbackInterface.OnMoveDown;
            }
            m_Wrapper.m_PieceActionsCallbackInterface = instance;
            if (instance != null)
            {
                @MoveLeft.started += instance.OnMoveLeft;
                @MoveLeft.performed += instance.OnMoveLeft;
                @MoveLeft.canceled += instance.OnMoveLeft;
                @MoveRight.started += instance.OnMoveRight;
                @MoveRight.performed += instance.OnMoveRight;
                @MoveRight.canceled += instance.OnMoveRight;
                @RotateAnticlockwise.started += instance.OnRotateAnticlockwise;
                @RotateAnticlockwise.performed += instance.OnRotateAnticlockwise;
                @RotateAnticlockwise.canceled += instance.OnRotateAnticlockwise;
                @RotateClockwise.started += instance.OnRotateClockwise;
                @RotateClockwise.performed += instance.OnRotateClockwise;
                @RotateClockwise.canceled += instance.OnRotateClockwise;
                @MoveDown.started += instance.OnMoveDown;
                @MoveDown.performed += instance.OnMoveDown;
                @MoveDown.canceled += instance.OnMoveDown;
            }
        }
    }
    public PieceActions @Piece => new PieceActions(this);
    public interface IPieceActions
    {
        void OnMoveLeft(InputAction.CallbackContext context);
        void OnMoveRight(InputAction.CallbackContext context);
        void OnRotateAnticlockwise(InputAction.CallbackContext context);
        void OnRotateClockwise(InputAction.CallbackContext context);
        void OnMoveDown(InputAction.CallbackContext context);
    }
}
