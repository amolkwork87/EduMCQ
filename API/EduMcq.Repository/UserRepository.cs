using EduMcq.Models;

namespace EduMcq.Repository;

public class UserRepository
{
    public UserRepository()
    {

    }

    public Task<bool> CreateUser(User req)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteUser(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetUserByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<User>> GetUsersAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUser(Guid id, User req)
    {
        throw new NotImplementedException();
    }
}
