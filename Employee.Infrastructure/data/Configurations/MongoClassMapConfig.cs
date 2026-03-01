using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Employee.Domain.Entities.Common;

namespace Employee.Infrastructure.data.Configurations
{
    public static class MongoClassMapConfig
    {
        private static readonly object _classMapLock = new();

        public static void Configure()
        {
            // 1. Cấu hình chung cho BaseEntity (Áp dụng cho TOÀN BỘ các bảng)
            // Lock guards against concurrent calls (e.g. integration test host initialisation).
            lock (_classMapLock)
            {
                if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
                {
                    BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true); // Quan trọng: Bỏ qua trường thừa để không lỗi khi update DB

                        // Map Id string sang ObjectId cho TẤT CẢ các bảng con
                        cm.MapIdMember(c => c.Id)
                          .SetIdGenerator(StringObjectIdGenerator.Instance)
                          .SetSerializer(new StringSerializer(BsonType.ObjectId));

                        // Prevent DomainEvents list from being persisted to MongoDB.
                        // MongoMappingConfig also does this but its BaseEntity block is skipped
                        // when this class runs first (IsClassMapRegistered returns true).
                        cm.UnmapProperty(c => c.DomainEvents);
                    });
                }
            }

            // 2. Cấu hình Global cho kiểu Decimal (Tiền tệ/Lương)
            // Mặc định Mongo lưu decimal là String, ta ép nó lưu là Decimal128 để tính toán được
            try
            {
                BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            }
            catch
            {
                // Bỏ qua nếu đã đăng ký rồi
            }
        }
    }
}