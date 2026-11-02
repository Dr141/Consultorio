-- Script de inicialização do PostgreSQL para múltiplos bancos
SELECT 'CREATE DATABASE "Identity"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'Identity')\gexec
