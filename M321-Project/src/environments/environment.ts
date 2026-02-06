export const environment = {
  production: false,
  keycloak: {
    issuer: 'http://localhost:18080/realms/pixelboard-test',
    clientId: 'student_client',
    // Hier ist das Secret jetzt zentralisiert (aber im Browser immer noch lesbar!)
    dummyClientSecret: 'W3O3oYj1Qny3ATcvE6nrozLe17KyW6e9'
  }
};