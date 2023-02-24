namespace FirstPersonController
{
    public class PlayerState
    {
        public virtual void Enter(Player player) { }
        public virtual void Exit(Player player) { }
        public virtual void Update(Player player) { }
        public virtual void FixedUpdate(Player player) { }

        public static PlayerState Walking = new PlayerStateWalking();
        public static PlayerState Flying = new PlayerStateFlying();
        public static PlayerState Swimming = new PlayerStateSwimming();
        public static PlayerState Climbing = new PlayerStateClimbing();
    }
}