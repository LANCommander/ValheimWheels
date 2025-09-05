using System.Collections.Generic;
using System.Linq;

namespace EquipmentWheel
{
    public sealed class WheelManager
    { 
        private static readonly HashSet<IWheel> _wheels = new HashSet<IWheel>();
        private static IWheel _activeWheel;
        
        public static bool InventoryVisible = false;
        public static bool HoverTextVisible = false;
        public static bool PressedOnHovering = false;

        public enum DPadButton
        {
            None,
            Left,
            Right,
            LeftOrRight
        }

        public static bool AnyVisible
        {
            get
            {
                return _wheels.Any(w => w.IsVisible());
            }
        }

        public static void Activate(IWheel wheel)
        {
            if (!wheel.IsVisible())
                return;

            foreach (var w in _wheels)
            {
                if (!wheel.Equals(w))
                {
                    w.Hide();
                }
            }

            _activeWheel = wheel;
        }
        
        public static bool IsActive(IWheel wheel)
        {
            return wheel.Equals(_activeWheel);
        }

        public static bool AddWheel(IWheel wheel)
        {
            return _wheels.Add(wheel);
        }

        public static bool RemoveWheel(IWheel wheel)
        {
            return _wheels.Remove(wheel);
        }

        public static bool BestMatchDown(IWheel wheel)
        {
            if (!_wheels.Contains(wheel))
                return false;

            var result = _wheels.OrderByDescending(w => w.GetKeyCountDown()).FirstOrDefault();

            return wheel.Equals(result);
        }

        public static bool BestMatchPressed(IWheel wheel)
        {
            if (!_wheels.Contains(wheel))
                return false;

            var result = _wheels.OrderByDescending(w => w.GetKeyCountPressed()).FirstOrDefault();

            return wheel.Equals(result);
        }

        public static float GetJoyStickIgnoreTime ()
        {
            float time = 0;

            foreach (var w in _wheels)
            {
                time += w.JoyStickIgnoreTime();
            }

            return time;
        }
    }
}
