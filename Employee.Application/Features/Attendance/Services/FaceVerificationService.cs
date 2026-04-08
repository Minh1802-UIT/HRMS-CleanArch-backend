using System;
using System.Linq;

namespace Employee.Application.Features.Attendance.Services
{
    /// <summary>
    /// Server-side face embedding comparison using cosine similarity.
    /// Embeddings are 128-float vectors extracted by face-api.js on the client.
    /// </summary>
    public class FaceVerificationService
    {
        /// <summary>
        /// Computes cosine similarity between two face embeddings.
        /// Returns a value between -1 and 1 (higher = more similar).
        /// Typical threshold for same-person: >= 0.6
        /// </summary>
        public double ComputeSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
                throw new ArgumentException("Embeddings must have the same dimension.");

            double dotProduct = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                norm1 += embedding1[i] * embedding1[i];
                norm2 += embedding2[i] * embedding2[i];
            }

            var denominator = Math.Sqrt(norm1) * Math.Sqrt(norm2);
            if (denominator == 0) return 0;

            return dotProduct / denominator;
        }

        /// <summary>
        /// Verifies if a check-in embedding matches the registered embedding.
        /// </summary>
        public FaceMatchResult Verify(float[] checkInEmbedding, float[] registeredEmbedding, double threshold = 0.6)
        {
            var similarity = ComputeSimilarity(checkInEmbedding, registeredEmbedding);
            return new FaceMatchResult
            {
                Similarity = Math.Round(similarity, 4),
                IsMatch = similarity >= threshold,
                Threshold = threshold
            };
        }
    }

    public class FaceMatchResult
    {
        public double Similarity { get; set; }
        public bool IsMatch { get; set; }
        public double Threshold { get; set; }
    }
}
