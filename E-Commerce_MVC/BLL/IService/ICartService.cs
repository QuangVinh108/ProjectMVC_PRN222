using DAL.Entities;

namespace BLL.IService
{
    public interface ICartService
    {
        Cart GetCart(int userId);
        void AddItem(int userId, int productId, int quantity);
        void UpdateItem(int cartItemId, int quantity);
        void RemoveItem(int cartItemId);
        Task AddOrReplaceSingleItemAsync(int userId, int productId, int quantity);
    }
}
