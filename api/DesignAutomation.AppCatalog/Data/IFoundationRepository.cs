using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Data;

public interface IFoundationRepository
{
    Task<List<FoundationProductSummary>> ListProducts();
    Task<Guid> CreateProduct(FoundationProductRequest request);
    Task<bool> UpdateProduct(Guid productId, FoundationProductRequest request);
}
