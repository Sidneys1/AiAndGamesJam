using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AiAndGamesJam {
    public class InputManager : GameComponent {
        private static MouseState _mouseStateA, _mouseStateB;
        private static bool isMouseSwapped;
        public static ref MouseState CurrentMouseState {
            get {
                if (isMouseSwapped)
                    return ref _mouseStateB;
                return ref _mouseStateA;
            }
        }
        public static ref MouseState LastMouseState {
            get {
                if (isMouseSwapped)
                    return ref _mouseStateA;
                return ref _mouseStateB;
            }
        }

        private static KeyboardState _keyboardStateA, _keyboardStateB;
        private static bool isKeyboardSwapped;

        public static ref KeyboardState CurrentKeyboardState {
            get {
                if (isKeyboardSwapped)
                    return ref _keyboardStateB;
                return ref _keyboardStateA;
            }
        }
        public static ref KeyboardState LastKeyboardState {
            get {
                if (isKeyboardSwapped)
                    return ref _keyboardStateA;
                return ref _keyboardStateB;
            }
        }

        public static bool KeyWentUp(Keys k) => LastKeyboardState.IsKeyDown(k) && CurrentKeyboardState.IsKeyUp(k);
        public static bool KeyWentDown(Keys k) => LastKeyboardState.IsKeyUp(k) && CurrentKeyboardState.IsKeyDown(k);

        public InputManager(Game game) : base(game) => UpdateOrder = 0;

        public override void Update(GameTime gameTime) {
            if (isMouseSwapped) {
                _mouseStateA = Mouse.GetState();
            } else {
                _mouseStateB = Mouse.GetState();
            }
            isMouseSwapped = !isMouseSwapped;

            if (isKeyboardSwapped) {
                _keyboardStateA = Keyboard.GetState();
            } else {
                _keyboardStateB = Keyboard.GetState();
            }
            isKeyboardSwapped = !isKeyboardSwapped;
        }

        internal static bool LeftMouseWentDown() =>
            LastMouseState.LeftButton == ButtonState.Released && CurrentMouseState.LeftButton == ButtonState.Pressed;
    }
}
