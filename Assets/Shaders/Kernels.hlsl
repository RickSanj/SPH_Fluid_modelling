static const float PI = 3.14159265f;

float PolynomialKernel(float dst, float radius)
{
	if (dst >= 0 && dst <= radius)
	{
		float coefA = 4.0 / (PI*pow(dst, 8.0));
		float v = dst*dst - radius*radius;
		return coefA * pow(v, 3.0);
	}
	return 0;
}

float LaplacianPolynomialKernel(float dst, float radius)
{
	if (dst >= 0 && dst <= radius)
	{
		float coefC = 24.0 / (PI * pow(dst, 8.0));
		return -coefC * (dst*dst - radius*radius) * (3*dst*dst - 7*radius*radius);
	}
	return 0;
}

float SpikyKernel(float dst, float radius)
{
	if (dst >= 0 && dst <= radius)
	{
		float coefA = 10.0 / (PI*pow(dst, 5.0));
		float v = dst - radius;
		return coefA * pow(v, 3.0);
	}
	return 0;
}


float ViscosityKernel(float dst, float radius)
{
	if (dst >= 0 && dst <= radius)
	{
		float coefA = 10.0 / (3*PI*dst*dst);
		float part1 = pow(radius, 3.0) / (2*pow(dst, 3.0));
		float part2 = pow(radius, 2.0) / (pow(dst, 2.0));
		float part3 = dst / 2*radius;
		return coefA * (-part1 + part2 + part3 - 1);
	}
	return 0;
}

float LaplacianViscosityKernel(float dst, float radius)
{
	if (dst >= 0 && dst <= radius)
	{
		float coefC = 20.0 / (PI*pow(dst, 5.0));
		return -coefC * (dst - radius);
	}
	return 0;
}
