using DAL.Entities;

namespace DAL.IRepository
{
    public interface ICartRepository
    {
        Cart GetCartByUserId(int userId);
        void AddItem(int userId, int productId, int quantity);
        void UpdateQuantity(int cartItemId, int quantity);
        void RemoveItem(int cartItemId);
    }
}
